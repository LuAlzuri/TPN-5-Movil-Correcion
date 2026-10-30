using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Security;

namespace TPN_5_MOVIL.Services;

public interface IAuditoriaAccionesService
{
    void Registrar(string accion, string entidad, string? entidadId, string? observacion = null);
    Task RegistrarAsync(string accion, string entidad, string? entidadId, string? observacion = null, CancellationToken ct = default);
}
