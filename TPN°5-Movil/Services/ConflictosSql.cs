using Microsoft.Data.SqlClient;

namespace TPN_5_MOVIL.Services;

internal static class ConflictosSql
{
    public static bool EsConcurrencia(Exception error)
    {
        // EF Core can wrap SQL Server transient errors in InvalidOperationException.
        // Only known deadlocks/lock timeouts are conflicts; do not hide other failures.
        for (Exception? current = error; current is not null; current = current.InnerException)
            if (current is SqlException sql && sql.Errors.Cast<SqlError>().Any(e => e.Number is 1205 or 1222))
                return true;
        return false;
    }
}
