using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Exceptions;

namespace TPN_5_MOVIL.Services;

public sealed class SalidaService(AppDbContext db)
{
    private const decimal MaxImporte = 9999999999999999.99m;

    public async Task<PaginatedResult<SalidaResponse>> ListarAsync(int? propietarioId, int page, int pageSize,
        int? productoId, int? clienteId, DateTime? desde, DateTime? hasta, CancellationToken ct)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new ApiException("Página inválida: page debe ser positivo y pageSize debe estar entre 1 y 100.");
        if (productoId <= 0 || clienteId <= 0 || desde > hasta)
            throw new ApiException("Revise los identificadores y el rango de fechas.");
        var query = Visibles(propietarioId);
        if (productoId.HasValue) query = query.Where(s => s.Detalles.Any(d => d.ProductoId == productoId.Value));
        if (clienteId.HasValue) query = query.Where(s => s.ClienteId == clienteId.Value);
        if (desde.HasValue) query = query.Where(s => s.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(s => s.Fecha <= hasta.Value);
        var count = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(s => s.Fecha).ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var ids = rows.Select(s => s.Id).ToArray();
        // Scope by visible headers; deleted products/accounts must not hide history.
        var details = await db.DetallesSalida.IgnoreQueryFilters().AsNoTracking()
            .Where(d => ids.Contains(d.SalidaId)).OrderBy(d => d.Id).ToListAsync(ct);
        var lookup = details.ToLookup(d => d.SalidaId);
        var totalPages = (int)Math.Ceiling(count / (double)pageSize);
        return new PaginatedResult<SalidaResponse>
        {
            Page = page, PageSize = pageSize, TotalCount = count, TotalPages = totalPages,
            HasNext = page < totalPages, HasPrevious = page > 1,
            Items = rows.Select(s => Respuesta(s, lookup[s.Id])).ToList()
        };
    }

    public async Task<SalidaResponse> ConsultarAsync(int id, int? propietarioId, CancellationToken ct) =>
        Respuesta(await BuscarAsync(id, propietarioId, ct), await DetallesAsync(id, ct));

    public async Task<DetalleSalidaResponse> ConsultarDetalleAsync(int id, int detalleId, int? propietarioId, CancellationToken ct)
    {
        await BuscarAsync(id, propietarioId, ct);
        return Respuesta(await BuscarDetalleAsync(id, detalleId, ct));
    }

    public Task<SalidaResponse> CrearAsync(SalidaRequest request, int clienteId, CancellationToken ct)
    {
        Validar(request);
        return TransaccionAsync(clienteId, true, async () =>
        {
            var header = new Salida
            {
                ClienteId = clienteId, Fecha = DateTime.UtcNow,
                Observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim()
            };
            db.Salidas.Add(header);
            await db.SaveChangesAsync(ct);
            return Respuesta(header, []);
        }, ct);
    }

    public Task<(DetalleSalidaResponse? Detalle, bool Creado)> AgregarDetalleAsync(int id, DetalleSalidaCreateRequest request,
        int clienteId, CancellationToken ct)
    {
        Validar(request);
        return TransaccionAsync(clienteId, true, async () =>
        {
            ExigirBorrador(await BloquearAsync(id, clienteId, ct));
            var details = await DetallesAsync(id, ct);
            var detail = details.SingleOrDefault(d => d.ProductoId == request.ProductoId!.Value);
            if (request.Cantidad == 0)
            {
                if (detail is not null)
                    ExigirCambio(await db.DetallesSalida.IgnoreQueryFilters().Where(d => d.SalidaId == id && d.Id == detail.Id)
                        .ExecuteDeleteAsync(ct));
                return ((DetalleSalidaResponse?)null, false);
            }
            var product = await db.Productos.AsNoTracking().SingleOrDefaultAsync(p => p.Id == request.ProductoId!.Value, ct)
                ?? throw NoEncontrado("El producto no existe o está eliminado.");
            if (request.Cantidad > product.Stock)
                throw Conflicto("La cantidad solicitada supera el stock disponible.");
            var created = detail is null;
            if (created)
            {
                detail = new DetalleSalida { SalidaId = id, ProductoId = product.Id, PrecioUnitario = product.Precio, DescAplicado = 0 };
                details.Add(detail);
            }
            detail!.Cantidad = request.Cantidad!.Value;
            ValidarImportes(details);
            if (created)
            {
                db.DetallesSalida.Add(detail);
                await db.SaveChangesAsync(ct);
            }
            else
                ExigirCambio(await db.DetallesSalida.IgnoreQueryFilters().Where(d => d.SalidaId == id && d.Id == detail.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(d => d.Cantidad, detail.Cantidad), ct));
            return ((DetalleSalidaResponse?)Respuesta(detail), created);
        }, ct);
    }

    public Task<DetalleSalidaResponse?> EditarDetalleAsync(int id, int detalleId, DetalleSalidaUpdateRequest request,
        int clienteId, CancellationToken ct)
    {
        Validar(request);
        return TransaccionAsync(clienteId, true, async () =>
        {
            ExigirBorrador(await BloquearAsync(id, clienteId, ct));
            var detail = await BuscarDetalleAsync(id, detalleId, ct);
            if (request.Cantidad == 0)
            {
                ExigirCambio(await db.DetallesSalida.IgnoreQueryFilters().Where(d => d.SalidaId == id && d.Id == detalleId)
                    .ExecuteDeleteAsync(ct));
                return (DetalleSalidaResponse?)null;
            }
            var product = await db.Productos.AsNoTracking().SingleOrDefaultAsync(p => p.Id == detail.ProductoId, ct)
                ?? throw NoEncontrado("El producto no existe o está eliminado.");
            if (request.Cantidad > product.Stock)
                throw Conflicto("La cantidad solicitada supera el stock disponible.");
            detail.Cantidad = request.Cantidad!.Value;
            ValidarImportes((await DetallesAsync(id, ct)).Where(d => d.Id != detalleId).Append(detail));
            ExigirCambio(await db.DetallesSalida.IgnoreQueryFilters().Where(d => d.SalidaId == id && d.Id == detalleId)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.Cantidad, detail.Cantidad), ct));
            return (DetalleSalidaResponse?)Respuesta(detail);
        }, ct);
    }

    public async Task QuitarDetalleAsync(int id, int detalleId, int clienteId, CancellationToken ct) =>
        await TransaccionAsync(clienteId, true, async () =>
        {
            ExigirBorrador(await BloquearAsync(id, clienteId, ct));
            await BuscarDetalleAsync(id, detalleId, ct);
            ExigirCambio(await db.DetallesSalida.IgnoreQueryFilters().Where(d => d.SalidaId == id && d.Id == detalleId)
                .ExecuteDeleteAsync(ct));
            return true;
        }, ct);

    public Task<SalidaResponse> ConfirmarClienteAsync(int id, int clienteId, CancellationToken ct) =>
        TransaccionAsync(clienteId, true, async () =>
        {
            ExigirBorrador(await BloquearAsync(id, clienteId, ct));
            await ValidarConfirmacionAsync(id, ct);
            var now = DateTime.UtcNow;
            ExigirCambio(await db.Salidas.Where(s => s.Id == id && !s.ConfirmarXCliente && !s.ConfirmarXEmpleado)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ConfirmarXCliente, true)
                    .SetProperty(x => x.FechaConfirmacionCliente, now), ct));
            return await ConsultarAsync(id, clienteId, ct);
        }, ct);

    public Task<SalidaResponse> ConfirmarAsync(int id, int usuarioId, CancellationToken ct) =>
        TransaccionAsync(usuarioId, false, async () =>
        {
            var header = await BloquearAsync(id, null, ct);
            if (!header.ConfirmarXCliente || header.ConfirmarXEmpleado)
                throw Conflicto("La salida debe estar confirmada por el cliente y pendiente de confirmación operativa.");
            if (!await db.Clientes.AnyAsync(c => c.Id == header.ClienteId && c.Usuario.Rol == Usuario.RolCliente && c.Usuario.Empleado == null, ct))
                throw Conflicto("El cliente no está disponible para confirmar la salida.");
            var details = await ValidarConfirmacionAsync(id, ct);
            var now = DateTime.UtcNow;
            ExigirCambio(await db.Salidas.Where(s => s.Id == id && s.ConfirmarXCliente && !s.ConfirmarXEmpleado)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ConfirmarXEmpleado, true)
                    .SetProperty(x => x.FechaConfirmacionEmpleado, now)
                    .SetProperty(x => x.UsuarioConfirmacionId, usuarioId), ct));
            foreach (var detail in details.OrderBy(d => d.ProductoId))
            {
                var affected = await db.Productos.Where(p => p.Id == detail.ProductoId && p.Stock >= detail.Cantidad)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - detail.Cantidad), ct);
                if (affected != 1) throw Conflicto("Un producto está eliminado o no tiene stock suficiente; no se confirmó la salida.");
            }
            return await ConsultarAsync(id, null, ct);
        }, ct);

    public async Task EliminarAsync(int id, int clienteId, CancellationToken ct) =>
        await TransaccionAsync(clienteId, true, async () =>
        {
            var header = await BloquearAsync(id, clienteId, ct);
            if (header.ConfirmarXEmpleado) throw Conflicto("Una salida confirmada operativamente no puede eliminarse.");
            ExigirCambio(await db.Salidas.Where(s => s.Id == id && !s.ConfirmarXEmpleado)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Eliminado, true), ct));
            return true;
        }, ct);

    private IQueryable<Salida> Visibles(int? propietarioId) => db.Salidas.AsNoTracking()
        .Where(s => !propietarioId.HasValue || s.ClienteId == propietarioId.Value);

    private async Task<Salida> BuscarAsync(int id, int? propietarioId, CancellationToken ct) =>
        await Visibles(propietarioId).SingleOrDefaultAsync(s => s.Id == id, ct) ?? throw NoEncontrado("La salida no existe.");

    private async Task<Salida> BloquearAsync(int id, int? propietarioId, CancellationToken ct)
    {
        var affected = await db.Salidas.Where(s => s.Id == id && (!propietarioId.HasValue || s.ClienteId == propietarioId.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Observaciones, x => x.Observaciones), ct);
        if (affected != 1) throw NoEncontrado("La salida no existe.");
        return await BuscarAsync(id, propietarioId, ct);
    }

    private Task<List<DetalleSalida>> DetallesAsync(int id, CancellationToken ct) =>
        db.DetallesSalida.IgnoreQueryFilters().AsNoTracking().Where(d => d.SalidaId == id).OrderBy(d => d.Id).ToListAsync(ct);

    private async Task<DetalleSalida> BuscarDetalleAsync(int id, int detalleId, CancellationToken ct) =>
        await db.DetallesSalida.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(d => d.SalidaId == id && d.Id == detalleId, ct)
        ?? throw NoEncontrado("El detalle no pertenece a la salida o no existe.");

    private async Task<List<DetalleSalida>> ValidarConfirmacionAsync(int id, CancellationToken ct)
    {
        var details = await DetallesAsync(id, ct);
        if (details.Count == 0) throw Conflicto("No se puede confirmar una salida sin detalles.");
        ValidarImportes(details);
        var ids = details.Select(d => d.ProductoId).Distinct().ToArray();
        if (ids.Length != details.Count) throw Conflicto("Un producto está repetido en la salida.");
        var products = await db.Productos.AsNoTracking().Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Stock }).ToDictionaryAsync(p => p.Id, p => p.Stock, ct);
        if (products.Count != ids.Length)
            throw Conflicto("Un producto de la salida ya no está disponible.");
        if (details.Any(d => d.Cantidad > products[d.ProductoId]))
            throw Conflicto("La cantidad solicitada supera el stock disponible.");
        return details;
    }

    private async Task<T> TransaccionAsync<T>(int usuarioId, bool cliente, Func<Task<T>> action, CancellationToken ct)
    {
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var consistent = cliente
                ? await db.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Rol == Usuario.RolCliente && u.Cliente != null && u.Empleado == null, ct)
                : await db.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Cliente == null &&
                    ((u.Rol == Usuario.RolAdministrador && u.Empleado == null) || (u.Rol == Usuario.RolEmpleado && u.Empleado != null)), ct);
            if (!consistent) throw new ApiException("La cuenta no tiene un perfil activo y autorizado para esta operación.", StatusCodes.Status403Forbidden);
            var result = await action();
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (Exception ex) when (ConflictosSql.EsConcurrencia(ex))
        { throw Conflicto("La operación coincidió con otra modificación; vuelva a intentarla."); }
    }

    private static void Validar(object request)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
            throw new ApiException("Datos de salida inválidos: " + string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }

    private static void ExigirBorrador(Salida header)
    {
        if (header.ConfirmarXCliente || header.ConfirmarXEmpleado)
            throw Conflicto("La salida ya fue solicitada; sus detalles están bloqueados.");
    }

    private static void ExigirCambio(int affected)
    {
        if (affected != 1) throw Conflicto("El estado cambió durante la operación.");
    }
    private static ApiException Conflicto(string message) => new(message, StatusCodes.Status409Conflict);
    private static ApiException NoEncontrado(string message) => new(message, StatusCodes.Status404NotFound);
    private static decimal Subtotal(DetalleSalida detail) =>
        decimal.Round(detail.Cantidad * detail.PrecioUnitario * (1 - detail.DescAplicado / 100m), 2, MidpointRounding.AwayFromZero);
    private static void ValidarImportes(IEnumerable<DetalleSalida> details)
    {
        decimal total = 0;
        foreach (var d in details)
        {
            if (d.Cantidad < 1 || d.PrecioUnitario < 0 || d.PrecioUnitario > MaxImporte
                || decimal.Round(d.PrecioUnitario, 2) != d.PrecioUnitario
                || d.DescAplicado < 0 || d.DescAplicado > 100 || decimal.Round(d.DescAplicado, 2) != d.DescAplicado)
                throw new ApiException("Un detalle contiene cantidades, precios o descuentos inválidos.");
            var subtotal = Subtotal(d);
            if (subtotal > MaxImporte || subtotal > MaxImporte - total)
                throw new ApiException("El total de la salida excede el rango decimal(18,2).");
            total += subtotal;
        }
    }
    private static DetalleSalidaResponse Respuesta(DetalleSalida d) =>
        new(d.Id, d.ProductoId, d.Cantidad, d.PrecioUnitario, d.DescAplicado, Subtotal(d));
    private static SalidaResponse Respuesta(Salida s, IEnumerable<DetalleSalida> details)
    {
        var rows = details.Select(Respuesta).ToList();
        return new(s.Id, s.ClienteId, s.UsuarioConfirmacionId, s.Fecha, s.Observaciones, s.ConfirmarXCliente,
            s.ConfirmarXEmpleado, s.FechaConfirmacionCliente, s.FechaConfirmacionEmpleado, rows, rows.Sum(d => d.Subtotal));
    }
}
