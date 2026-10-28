using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Exceptions;
using TPN_5_MOVIL.Services;

namespace TPN_5_MOVIL.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize(Roles = Usuario.RolAdministrador)]
public sealed class ReportesController(AppDbContext db, IReportePdfService pdf) : ControllerBase
{
    public const int MaxFilas = 1000;
    public const int MaxDetalles = 2000;

    [HttpGet("stock")]
    public async Task<IActionResult> Stock(CancellationToken ct)
    {
        var rows = await db.Productos.IgnoreQueryFilters().AsNoTracking().Where(p => !p.Eliminado)
            .OrderBy(p => p.Nombre).ThenBy(p => p.Id)
            .Select(p => new ReporteStockFila(p.Id, p.Nombre, p.Categoria.Nombre, p.Precio, p.Stock))
            .Take(MaxFilas + 1).ToListAsync(ct);
        ExigirLimite(rows.Count, MaxFilas, "productos");
        ct.ThrowIfCancellationRequested();
        return File(pdf.GenerarStock(rows, new(DateTimeOffset.UtcNow)), "application/pdf", "stock.pdf");
    }

    [HttpGet("ingresos")]
    public async Task<IActionResult> Ingresos([FromQuery] ReporteFechasFiltro filtro, CancellationToken ct)
    {
        var query = db.Ingresos.IgnoreQueryFilters().AsNoTracking().Where(i => i.Confirmado && !i.Eliminado);
        if (filtro.ObtenerDesdeUtc() is DateTime desde) query = query.Where(i => i.FechaConfirmacion >= desde);
        if (filtro.ObtenerHastaExclusivoUtc() is DateTime hasta) query = query.Where(i => i.FechaConfirmacion < hasta);
        var headers = await query.OrderBy(i => i.FechaConfirmacion).ThenBy(i => i.Id)
            .Select(i => new Cabecera(i.Id, i.FechaConfirmacion!.Value, i.Proveedor.Nombre,
                i.EmpleadoConfirmacion!.Usuario.Nombre + " " + i.EmpleadoConfirmacion.Usuario.Apellido))
            .Take(MaxFilas + 1).ToListAsync(ct);
        ExigirLimite(headers.Count, MaxFilas, "ingresos; reduzca el rango de fechas");
        var ids = headers.Select(i => i.Id).ToArray();
        var details = await db.DetallesIngreso.IgnoreQueryFilters().AsNoTracking().Where(d => ids.Contains(d.IngresoId))
            .OrderBy(d => d.IngresoId).ThenBy(d => d.Id)
            .Select(d => new Detalle(d.IngresoId, d.Cantidad, d.PrecioUnitario, d.DescAplicado))
            .Take(MaxDetalles + 1).ToListAsync(ct);
        ExigirLimite(details.Count, MaxDetalles, "detalles de ingresos; reduzca el rango de fechas");
        var rows = Completar(headers, details);
        ct.ThrowIfCancellationRequested();
        return File(pdf.GenerarIngresos(rows, new(DateTimeOffset.UtcNow, filtro.Desde, filtro.Hasta)), "application/pdf", "ingresos.pdf");
    }

    [HttpGet("salidas")]
    public async Task<IActionResult> Salidas([FromQuery] ReporteFechasFiltro filtro, CancellationToken ct)
    {
        var query = db.Salidas.IgnoreQueryFilters().AsNoTracking().Where(s => s.ConfirmarXEmpleado && !s.Eliminado);
        if (filtro.ObtenerDesdeUtc() is DateTime desde) query = query.Where(s => s.FechaConfirmacionEmpleado >= desde);
        if (filtro.ObtenerHastaExclusivoUtc() is DateTime hasta) query = query.Where(s => s.FechaConfirmacionEmpleado < hasta);
        var headers = await query.OrderBy(s => s.FechaConfirmacionEmpleado).ThenBy(s => s.Id)
            .Select(s => new Cabecera(s.Id, s.FechaConfirmacionEmpleado!.Value,
                s.Cliente.Usuario.Nombre + " " + s.Cliente.Usuario.Apellido,
                s.UsuarioConfirmacion!.Nombre + " " + s.UsuarioConfirmacion.Apellido))
            .Take(MaxFilas + 1).ToListAsync(ct);
        ExigirLimite(headers.Count, MaxFilas, "salidas; reduzca el rango de fechas");
        var ids = headers.Select(s => s.Id).ToArray();
        var details = await db.DetallesSalida.IgnoreQueryFilters().AsNoTracking().Where(d => ids.Contains(d.SalidaId))
            .OrderBy(d => d.SalidaId).ThenBy(d => d.Id)
            .Select(d => new Detalle(d.SalidaId, d.Cantidad, d.PrecioUnitario, d.DescAplicado))
            .Take(MaxDetalles + 1).ToListAsync(ct);
        ExigirLimite(details.Count, MaxDetalles, "detalles de salidas; reduzca el rango de fechas");
        var rows = Completar(headers, details);
        ct.ThrowIfCancellationRequested();
        return File(pdf.GenerarSalidas(rows, new(DateTimeOffset.UtcNow, filtro.Desde, filtro.Hasta)), "application/pdf", "salidas.pdf");
    }

    private static List<ReporteMovimientoFila> Completar(List<Cabecera> headers, List<Detalle> details)
    {
        // Decimal's 96-bit range is retained: never validate the report sum as decimal(18,2).
        // The 2,000-detail cap also bounds even the largest legal SQL quantity/price combination.
        var totals = details.GroupBy(d => d.EncabezadoId).ToDictionary(g => g.Key,
            g => g.Sum(d => ImportesMovimiento.Subtotal(d.Cantidad, d.Precio, d.Descuento)));
        return headers.Select(h => new ReporteMovimientoFila(h.Id, h.Fecha, h.Contraparte, h.Confirmador,
            totals.GetValueOrDefault(h.Id))).ToList();
    }
    private static void ExigirLimite(int count, int max, string resource)
    {
        if (count > max) throw new ApiException($"El reporte supera el límite de {max} {resource}. No se generó un reporte parcial.");
    }
    private sealed record Cabecera(int Id, DateTime Fecha, string Contraparte, string Confirmador);
    private sealed record Detalle(int EncabezadoId, int Cantidad, decimal Precio, decimal Descuento);
}
