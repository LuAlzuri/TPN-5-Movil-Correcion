using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Exceptions;

namespace TPN_5_MOVIL.Data;

public static class Guardado
{
    public static async Task GuardarOConflictoAsync(this AppDbContext db, string mensaje,
        CancellationToken ct = default)
    {
        try
        {
            // One SaveChanges preserves atomic account/profile inserts and updates.
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ApiException(mensaje, StatusCodes.Status409Conflict);
        }
    }

    public static async Task ExigirEmailLibreAsync(this AppDbContext db, string email,
        string mensaje, int? excluirId = null, CancellationToken ct = default)
    {
        var comparison = email.ToUpperInvariant();
        if (await db.Usuarios.IgnoreQueryFilters()
            .AnyAsync(u => u.Email.ToUpper() == comparison && (!excluirId.HasValue || u.Id != excluirId.Value), ct))
            throw new ApiException(mensaje, StatusCodes.Status409Conflict);
    }
}
