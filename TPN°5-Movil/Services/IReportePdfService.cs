using TPN_5_MOVIL.DTOs;

namespace TPN_5_MOVIL.Services;

public interface IReportePdfService
{
    byte[] GenerarStock(IReadOnlyCollection<ReporteStockFila> filas, ReporteContexto contexto);
    byte[] GenerarIngresos(IReadOnlyCollection<ReporteMovimientoFila> filas, ReporteContexto contexto);
    byte[] GenerarSalidas(IReadOnlyCollection<ReporteMovimientoFila> filas, ReporteContexto contexto);
    byte[] GenerarComprobante(ReporteComprobanteSalida comprobante, ReporteContexto contexto);
}
