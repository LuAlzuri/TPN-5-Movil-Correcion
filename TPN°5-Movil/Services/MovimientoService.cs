using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Exceptions;

namespace TPN_5_MOVIL.Services;

// Transitional guard: legacy operations updated stock before confirmation.
// Replace with the transactional confirmation workflows in steps 10 and 11.
public class MovimientoService
{
    public Task<IngresoResponse> CrearIngresoAsync(IngresoRequest request) =>
        throw new ApiException("El flujo de ingresos por confirmación está pendiente de implementación.", StatusCodes.Status501NotImplemented);

    public Task<SalidaResponse> CrearSalidaAsync(SalidaRequest request) =>
        throw new ApiException("El flujo de carrito y confirmación de salidas está pendiente de implementación.", StatusCodes.Status501NotImplemented);
}
