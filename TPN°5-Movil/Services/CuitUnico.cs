using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Exceptions;

namespace TPN_5_MOVIL.Services;

public static class CuitUnico
{
    // Keep these separators aligned with the computed columns in AppDbContext.
    public static string Normalizar(string? cuit)
    {
        if (string.IsNullOrWhiteSpace(cuit) || cuit.Length > 20)
            throw new ApiException("El CUIT/CUIL es obligatorio y admite hasta 20 caracteres.");
        var normalizado = cuit.Replace("-", "").Replace(" ", "").Replace("\t", "")
            .Replace("\r", "").Replace("\n", "");
        if (normalizado.Length == 0)
            throw new ApiException("El CUIT/CUIL debe contener un identificador.");
        return normalizado;
    }

    public static async Task ValidarClienteAsync(AppDbContext db, string cuit, int? excluirId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizado = Normalizar(cuit);
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Rol == Usuario.RolCliente
            && (!excluirId.HasValue || u.Id != excluirId.Value)
            && EF.Property<string>(u, "CUITNormalizado") == normalizado, cancellationToken))
            throw new ApiException("Ya existe un cliente con ese CUIT/CUIL.", StatusCodes.Status409Conflict);
    }

    public static async Task ValidarProveedorAsync(AppDbContext db, string cuit, int? excluirId = null)
    {
        var normalizado = Normalizar(cuit);
        if (await db.Proveedores.IgnoreQueryFilters().AnyAsync(p => (!excluirId.HasValue || p.Id != excluirId.Value)
            && EF.Property<string>(p, "CUITNormalizado") == normalizado))
            throw new ApiException("Ya existe un proveedor con ese CUIT/CUIL.", StatusCodes.Status409Conflict);
    }
}
