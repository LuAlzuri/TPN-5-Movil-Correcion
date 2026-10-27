using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Exceptions;

namespace TPN_5_MOVIL.Services;

public sealed class IngresoService(AppDbContext db)
{
    private const decimal MaxImporte = 9999999999999999.99m;

    public async Task<PaginatedResult<IngresoResponse>> ListarAsync(int page, int pageSize,
        int? productoId, int? proveedorId, DateTime? desde, DateTime? hasta, CancellationToken ct)
    {
        Paginacion.Validar(page, pageSize);
        if (productoId <= 0 || proveedorId <= 0 || desde > hasta)
            throw new ApiException("Revise los identificadores y el rango de fechas.");

        var query = db.Ingresos.AsNoTracking();
        if (productoId.HasValue) query = query.Where(i => i.Detalles.Any(d => d.ProductoId == productoId.Value));
        if (proveedorId.HasValue) query = query.Where(i => i.ProveedorId == proveedorId.Value);
        if (desde.HasValue) query = query.Where(i => i.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(i => i.Fecha <= hasta.Value);
        var count = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(i => i.Fecha).ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var ids = rows.Select(i => i.Id).ToArray();
        // No required-navigation joins: deleted counterparties must not hide headers/details.
        var details = await db.DetallesIngreso.IgnoreQueryFilters().AsNoTracking()
            .Where(d => ids.Contains(d.IngresoId)).OrderBy(d => d.Id).ToListAsync(ct);
        var lookup = details.ToLookup(d => d.IngresoId);
        return PaginatedResult<IngresoResponse>.From(
            rows.Select(i => Respuesta(i, lookup[i.Id])).ToList(), count, page, pageSize);
    }

    public async Task<IngresoResponse> ConsultarAsync(int id, CancellationToken ct = default)
    {
        var header = await db.Ingresos.AsNoTracking().SingleOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new ApiException("El ingreso no existe.", StatusCodes.Status404NotFound);
        return Respuesta(header, await DetallesAsync(id, ct));
    }

    public async Task<DetalleIngresoResponse> ConsultarDetalleAsync(int id, int detalleId, CancellationToken ct)
    {
        if (!await db.Ingresos.AnyAsync(i => i.Id == id, ct))
            throw new ApiException("El ingreso no existe.", StatusCodes.Status404NotFound);
        return Respuesta(await BuscarDetalleAsync(id, detalleId, ct));
    }

    public Task<IngresoResponse> CrearAsync(IngresoRequest request, int empleadoId, CancellationToken ct)
    {
        Validar(request);
        return TransaccionAsync(empleadoId, async () =>
        {
            if (!await db.Proveedores.AnyAsync(p => p.Id == request.ProveedorId!.Value, ct))
                throw new ApiException("El proveedor no existe o está eliminado.", StatusCodes.Status404NotFound);
            var header = new Ingreso
            {
                ProveedorId = request.ProveedorId!.Value, EmpleadoId = empleadoId,
                Fecha = DateTime.UtcNow,
                Observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim()
            };
            db.Ingresos.Add(header);
            await db.SaveChangesAsync(ct);
            return Respuesta(header, []);
        }, ct);
    }

    public Task<DetalleIngresoResponse> AgregarDetalleAsync(int id, DetalleIngresoCreateRequest request, int empleadoId, CancellationToken ct)
    {
        Validar(request);
        return TransaccionAsync(empleadoId, async () =>
        {
            ExigirBorrador(await BloquearAsync(id, ct));
            var product = await db.Productos.AsNoTracking().SingleOrDefaultAsync(p => p.Id == request.ProductoId!.Value, ct)
                ?? throw new ApiException("El producto no existe o está eliminado.", StatusCodes.Status404NotFound);
            var detail = new DetalleIngreso
            {
                IngresoId = id, ProductoId = product.Id, Cantidad = request.Cantidad!.Value,
                PrecioUnitario = product.Precio, DescAplicado = request.DescAplicado
            };
            var details = await DetallesAsync(id, ct);
            details.Add(detail);
            ValidarImportes(details);
            db.DetallesIngreso.Add(detail);
            await db.SaveChangesAsync(ct);
            return Respuesta(detail);
        }, ct);
    }

    public Task<DetalleIngresoResponse> EditarDetalleAsync(int id, int detalleId, DetalleIngresoUpdateRequest request,
        int empleadoId, CancellationToken ct)
    {
        Validar(request);
        return TransaccionAsync(empleadoId, async () =>
        {
            ExigirBorrador(await BloquearAsync(id, ct));
            var detail = await BuscarDetalleAsync(id, detalleId, ct);
            if (!await db.Productos.AnyAsync(p => p.Id == detail.ProductoId, ct))
                throw new ApiException("El producto no existe o está eliminado.", StatusCodes.Status404NotFound);
            detail.Cantidad = request.Cantidad!.Value;
            detail.DescAplicado = request.DescAplicado;
            var others = await DetallesAsync(id, ct);
            ValidarImportes(others.Where(d => d.Id != detalleId).Append(detail));
            var changed = await db.DetallesIngreso.IgnoreQueryFilters()
                .Where(d => d.IngresoId == id && d.Id == detalleId)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.Cantidad, detail.Cantidad)
                    .SetProperty(d => d.DescAplicado, detail.DescAplicado), ct);
            ExigirCambio(changed);
            return Respuesta(detail);
        }, ct);
    }

    public async Task QuitarDetalleAsync(int id, int detalleId, int empleadoId, CancellationToken ct)
    {
        await TransaccionAsync(empleadoId, async () =>
        {
            ExigirBorrador(await BloquearAsync(id, ct));
            await BuscarDetalleAsync(id, detalleId, ct);
            ExigirCambio(await db.DetallesIngreso.IgnoreQueryFilters().Where(d => d.IngresoId == id && d.Id == detalleId)
                .ExecuteDeleteAsync(ct));
            return true;
        }, ct);
    }

    public Task<IngresoResponse> ConfirmarAsync(int id, int empleadoId, CancellationToken ct) =>
        TransaccionAsync(empleadoId, async () =>
        {
            var header = await BloquearAsync(id, ct);
            ExigirBorrador(header);
            var details = await DetallesAsync(id, ct);
            if (details.Count == 0) throw Conflicto("No se puede confirmar un ingreso sin detalles.");
            if (!await db.Proveedores.AnyAsync(p => p.Id == header.ProveedorId, ct))
                throw Conflicto("El proveedor no está disponible para confirmar.");
            ValidarImportes(details);
            var quantities = Agrupar(details);
            var now = DateTime.UtcNow;
            ExigirCambio(await db.Ingresos.Where(i => i.Id == id && !i.Confirmado)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Confirmado, true)
                    .SetProperty(i => i.FechaConfirmacion, now)
                    .SetProperty(i => i.EmpleadoConfirmacionId, empleadoId), ct));
            foreach (var (productoId, cantidad) in quantities)
            {
                var affected = await db.Productos.Where(p => p.Id == productoId && p.Stock <= int.MaxValue - cantidad)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + cantidad), ct);
                if (affected != 1) throw Conflicto("Un producto está eliminado o su stock excedería el máximo permitido.");
            }
            return await ConsultarAsync(id, ct);
        }, ct);

    public async Task EliminarAsync(int id, int empleadoId, CancellationToken ct)
    {
        await TransaccionAsync(empleadoId, async () =>
        {
            var header = await BloquearAsync(id, ct);
            var quantities = header.Confirmado ? Agrupar(await DetallesAsync(id, ct)) : [];
            ExigirCambio(await db.Ingresos.Where(i => i.Id == id && i.Confirmado == header.Confirmado)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Eliminado, true), ct));
            foreach (var (productoId, cantidad) in quantities)
            {
                // Historical stock must also be reversed on soft-deleted products.
                var affected = await db.Productos.IgnoreQueryFilters().Where(p => p.Id == productoId && p.Stock >= cantidad)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - cantidad), ct);
                if (affected != 1) throw Conflicto("No se puede revertir el ingreso: el stock de un producto es insuficiente.");
            }
            return true;
        }, ct);
    }

    private async Task<Ingreso> BloquearAsync(int id, CancellationToken ct)
    {
        var affected = await db.Ingresos.Where(i => i.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Observaciones, i => i.Observaciones), ct);
        if (affected != 1) throw new ApiException("El ingreso no existe.", StatusCodes.Status404NotFound);
        return await db.Ingresos.AsNoTracking().SingleAsync(i => i.Id == id, ct);
    }

    private Task<List<DetalleIngreso>> DetallesAsync(int id, CancellationToken ct) =>
        db.DetallesIngreso.IgnoreQueryFilters().AsNoTracking().Where(d => d.IngresoId == id).OrderBy(d => d.Id).ToListAsync(ct);

    private async Task<DetalleIngreso> BuscarDetalleAsync(int id, int detalleId, CancellationToken ct) =>
        await db.DetallesIngreso.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(d => d.IngresoId == id && d.Id == detalleId, ct)
        ?? throw new ApiException("El detalle no pertenece al ingreso o no existe.", StatusCodes.Status404NotFound);

    private async Task<T> TransaccionAsync<T>(int empleadoId, Func<Task<T>> action, CancellationToken ct)
    {
        try
        {
            await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            if (!await db.Empleados.AnyAsync(e => e.Id == empleadoId && e.Usuario.Rol == Usuario.RolEmpleado && e.Usuario.Cliente == null, ct))
                throw new ApiException("Se requiere un empleado activo y consistente.", StatusCodes.Status403Forbidden);
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
            throw new ApiException("Datos de ingreso inválidos: " + string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }

    private static void ExigirBorrador(Ingreso header)
    {
        if (header.Confirmado) throw Conflicto("El ingreso está confirmado; sus detalles están bloqueados.");
    }

    private static void ExigirCambio(int affected)
    {
        if (affected != 1) throw Conflicto("El estado cambió durante la operación.");
    }

    private static ApiException Conflicto(string message) => new(message, StatusCodes.Status409Conflict);

    private static List<(int ProductoId, int Cantidad)> Agrupar(List<DetalleIngreso> details) =>
        details.GroupBy(d => d.ProductoId).OrderBy(g => g.Key).Select(g =>
        {
            var quantity = g.Sum(d => (long)d.Cantidad);
            if (quantity < 1 || quantity > int.MaxValue) throw Conflicto("La cantidad acumulada de un producto excede el rango permitido.");
            return (g.Key, (int)quantity);
        }).ToList();

    private static decimal Subtotal(DetalleIngreso detail) =>
        decimal.Round(detail.Cantidad * detail.PrecioUnitario * (1 - detail.DescAplicado / 100m), 2, MidpointRounding.AwayFromZero);

    private static void ValidarImportes(IEnumerable<DetalleIngreso> details)
    {
        decimal total = 0;
        foreach (var detail in details)
        {
            if (detail.Cantidad < 1 || detail.PrecioUnitario < 0 || detail.PrecioUnitario > MaxImporte
                || decimal.Round(detail.PrecioUnitario, 2) != detail.PrecioUnitario
                || detail.DescAplicado < 0 || detail.DescAplicado > 100 || decimal.Round(detail.DescAplicado, 2) != detail.DescAplicado)
                throw new ApiException("Un detalle contiene cantidades, precios o descuentos inválidos.");
            var subtotal = Subtotal(detail);
            if (subtotal > MaxImporte || subtotal > MaxImporte - total)
                throw new ApiException("El total del ingreso excede el rango decimal(18,2).");
            total += subtotal;
        }
    }

    private static DetalleIngresoResponse Respuesta(DetalleIngreso d) =>
        new(d.Id, d.ProductoId, d.Cantidad, d.PrecioUnitario, d.DescAplicado, Subtotal(d));

    private static IngresoResponse Respuesta(Ingreso i, IEnumerable<DetalleIngreso> details)
    {
        var rows = details.Select(Respuesta).ToList();
        return new(i.Id, i.ProveedorId, i.EmpleadoId, i.Fecha, i.Observaciones, i.Confirmado,
            i.FechaConfirmacion, i.EmpleadoConfirmacionId, rows, rows.Sum(d => d.Subtotal));
    }
}
