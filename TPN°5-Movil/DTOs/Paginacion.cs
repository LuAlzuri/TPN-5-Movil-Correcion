using TPN_5_MOVIL.Exceptions;

namespace TPN_5_MOVIL.DTOs;

public static class Paginacion
{
    public const int MaxPageSize = 100;

    public static void Validar(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize || (long)(page - 1) * pageSize > int.MaxValue)
            throw new ApiException("Página inválida: page debe ser positivo y pageSize debe estar entre 1 y 100.");
    }
}
