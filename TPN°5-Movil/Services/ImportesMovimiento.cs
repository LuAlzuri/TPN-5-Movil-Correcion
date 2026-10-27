using TPN_5_MOVIL.Exceptions;

namespace TPN_5_MOVIL.Services;

public static class ImportesMovimiento
{
    private const decimal MaxImporte = 9999999999999999.99m;

    public static decimal Subtotal(int cantidad, decimal precioUnitario, decimal descuento) =>
        decimal.Round(cantidad * precioUnitario * (1 - descuento / 100m), 2, MidpointRounding.AwayFromZero);

    public static void Validar(IEnumerable<(int Cantidad, decimal PrecioUnitario, decimal DescAplicado)> detalles,
        string mensajeTotal)
    {
        decimal total = 0;
        foreach (var d in detalles)
        {
            if (d.Cantidad < 1 || d.PrecioUnitario < 0 || d.PrecioUnitario > MaxImporte
                || decimal.Round(d.PrecioUnitario, 2) != d.PrecioUnitario
                || d.DescAplicado < 0 || d.DescAplicado > 100 || decimal.Round(d.DescAplicado, 2) != d.DescAplicado)
                throw new ApiException("Un detalle contiene cantidades, precios o descuentos inválidos.");
            var subtotal = Subtotal(d.Cantidad, d.PrecioUnitario, d.DescAplicado);
            if (subtotal > MaxImporte || subtotal > MaxImporte - total)
                throw new ApiException(mensajeTotal);
            total += subtotal;
        }
    }
}
