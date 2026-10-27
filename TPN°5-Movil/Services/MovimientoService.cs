using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Exceptions;

namespace TPN_5_MOVIL.Services;

// The pending checkout workflow is implemented separately in step 11.
public class MovimientoService
{
    public Task<SalidaResponse> CrearSalidaAsync(SalidaRequest request) =>
        throw new ApiException("El flujo de carrito y confirmación de salidas está pendiente de implementación.", StatusCodes.Status501NotImplemented);
}
