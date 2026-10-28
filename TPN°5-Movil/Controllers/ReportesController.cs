using System.Globalization;
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
public sealed class ReportesController(AppDbContext db, IReportePdfService pdf, IServicioCorreo correo) : ControllerBase
{
    public const int MaxFilas = 1000;
    public const int MaxDetalles = 2000;
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    [HttpGet("stock")]
    public async Task<IActionResult> Stock(CancellationToken ct)
    {
        var filas = await ObtenerFilasStockAsync(ct);
        var pdfBytes = pdf.GenerarStock(filas, new(DateTimeOffset.UtcNow));
        return File(pdfBytes, "application/pdf", "stock.pdf");
    }

    [HttpGet("ingresos")]
    public async Task<IActionResult> Ingresos([FromQuery] ReporteFechasFiltro filtro, CancellationToken ct)
    {
        var (filas, contexto) = await ObtenerFilasIngresosAsync(filtro, ct);
        var pdfBytes = pdf.GenerarIngresos(filas, contexto);
        return File(pdfBytes, "application/pdf", "ingresos.pdf");
    }

    [HttpGet("salidas")]
    public async Task<IActionResult> Salidas([FromQuery] ReporteFechasFiltro filtro, CancellationToken ct)
    {
        var (filas, contexto) = await ObtenerFilasSalidasAsync(filtro, ct);
        var pdfBytes = pdf.GenerarSalidas(filas, contexto);
        return File(pdfBytes, "application/pdf", "salidas.pdf");
    }

    [HttpPost("{tipo}/enviar")]
    public async Task<IActionResult> Enviar(string tipo, [FromBody] ReporteEnviarRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            throw new ApiException("Tipo de reporte inválido. Tipos admitidos: stock, ingresos, salidas.", StatusCodes.Status400BadRequest);

        var tipoNormalizado = tipo.Trim().ToLowerInvariant();
        if (tipoNormalizado != "stock" && tipoNormalizado != "ingresos" && tipoNormalizado != "salidas")
            throw new ApiException("Tipo de reporte inválido. Tipos admitidos: stock, ingresos, salidas.", StatusCodes.Status400BadRequest);

        if (!ModelState.IsValid)
        {
            var errores = ModelState.Values.SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToList();
            var mensaje = errores.Count == 0 ? "Solicitud inválida." : string.Join(" ", errores);
            throw new ApiException(mensaje, StatusCodes.Status400BadRequest);
        }

        byte[] pdfBytes;
        string nombreAdjunto;
        string asunto;

        if (tipoNormalizado == "stock")
        {
            var filas = await ObtenerFilasStockAsync(ct);
            pdfBytes = pdf.GenerarStock(filas, new(DateTimeOffset.UtcNow));
            nombreAdjunto = $"stock-{DateTime.UtcNow:yyyy-MM-dd}.pdf";
            asunto = "Reporte de stock";
        }
        else if (tipoNormalizado == "ingresos")
        {
            var filtro = new ReporteFechasFiltro { Desde = request.Desde, Hasta = request.Hasta };
            ValidarFiltroFechas(filtro);
            var (filas, contexto) = await ObtenerFilasIngresosAsync(filtro, ct);
            pdfBytes = pdf.GenerarIngresos(filas, contexto);
            nombreAdjunto = GenerarNombreMovimientos("ingresos", filtro);
            asunto = "Reporte de ingresos";
        }
        else // salidas
        {
            var filtro = new ReporteFechasFiltro { Desde = request.Desde, Hasta = request.Hasta };
            ValidarFiltroFechas(filtro);
            var (filas, contexto) = await ObtenerFilasSalidasAsync(filtro, ct);
            pdfBytes = pdf.GenerarSalidas(filas, contexto);
            nombreAdjunto = GenerarNombreMovimientos("salidas", filtro);
            asunto = "Reporte de salidas";
        }

        ct.ThrowIfCancellationRequested();
        await correo.EnviarReportePdfAsync(request.Destinatario.Trim(), asunto, pdfBytes, nombreAdjunto, ct);
        return Ok(new ReporteEnviarResponse(true, "Correo enviado con éxito."));
    }

    private async Task<List<ReporteStockFila>> ObtenerFilasStockAsync(CancellationToken ct)
    {
        var rows = await db.Productos.IgnoreQueryFilters().AsNoTracking().Where(p => !p.Eliminado)
            .OrderBy(p => p.Nombre).ThenBy(p => p.Id)
            .Select(p => new ReporteStockFila(p.Id, p.Nombre, p.Categoria.Nombre, p.Precio, p.Stock))
            .Take(MaxFilas + 1).ToListAsync(ct);
        ExigirLimite(rows.Count, MaxFilas, "productos");
        ct.ThrowIfCancellationRequested();
        return rows.Take(MaxFilas).ToList();
    }

    private async Task<(List<ReporteMovimientoFila> Filas, ReporteContexto Contexto)> ObtenerFilasIngresosAsync(ReporteFechasFiltro filtro, CancellationToken ct)
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
        var rows = Completar(headers.Take(MaxFilas).ToList(), details);
        ct.ThrowIfCancellationRequested();
        return (rows, new(DateTimeOffset.UtcNow, filtro.Desde, filtro.Hasta));
    }

    private async Task<(List<ReporteMovimientoFila> Filas, ReporteContexto Contexto)> ObtenerFilasSalidasAsync(ReporteFechasFiltro filtro, CancellationToken ct)
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
        var rows = Completar(headers.Take(MaxFilas).ToList(), details);
        ct.ThrowIfCancellationRequested();
        return (rows, new(DateTimeOffset.UtcNow, filtro.Desde, filtro.Hasta));
    }

    private static void ValidarFiltroFechas(ReporteFechasFiltro filtro)
    {
        if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value > filtro.Hasta.Value)
            throw new ApiException("Desde no puede ser posterior a Hasta.", StatusCodes.Status400BadRequest);
        if (filtro.Hasta.HasValue && filtro.Hasta.Value == DateOnly.MaxValue)
            throw new ApiException("Hasta debe ser anterior a 9999-12-31 para incluir el día completo.", StatusCodes.Status400BadRequest);
    }

    private static string GenerarNombreMovimientos(string prefijo, ReporteFechasFiltro filtro)
    {
        if (filtro.Desde.HasValue && filtro.Hasta.HasValue)
            return $"{prefijo}-{filtro.Desde.Value:yyyy-MM-dd}_a_{filtro.Hasta.Value:yyyy-MM-dd}.pdf";
        if (filtro.Desde.HasValue)
            return $"{prefijo}-desde-{filtro.Desde.Value:yyyy-MM-dd}.pdf";
        if (filtro.Hasta.HasValue)
            return $"{prefijo}-hasta-{filtro.Hasta.Value:yyyy-MM-dd}.pdf";
        return $"{prefijo}-{DateTime.UtcNow:yyyy-MM-dd}.pdf";
    }

    private static List<ReporteMovimientoFila> Completar(List<Cabecera> headers, List<Detalle> details)
    {
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
