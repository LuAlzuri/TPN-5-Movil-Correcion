using TPN_5_MOVIL.Exceptions;
using TPN_5_MOVIL.Services;
using Xunit;

namespace TPN5.Integration;

public sealed class ImportesMovimientoTests
{
    private const decimal Max = 9999999999999999.99m;

    public static TheoryData<int, decimal, decimal, decimal> Subtotales => new()
    {
        { 1, 0.01m, 50m, 0.01m },
        { 3, 0.01m, 50m, 0.02m },
        { 1, 12.35m, 12.5m, 10.81m },
        { 1, 12.35m, 12.34m, 10.83m },
        { 1, Max, 0m, Max },
        { int.MaxValue, Max, 100m, 0m }
    };

    [Theory, MemberData(nameof(Subtotales))]
    public void Calcula_subtotal_con_redondeo_por_detalle(int cantidad, decimal precio, decimal descuento, decimal esperado) =>
        Assert.Equal(esperado, ImportesMovimiento.Subtotal(cantidad, precio, descuento));

    public static TheoryData<int, decimal, decimal> Invalidos => new()
    {
        { 0, 1m, 0m },
        { -1, 1m, 0m },
        { 1, -0.01m, 0m },
        { 1, Max + 0.01m, 0m },
        { 1, 0.001m, 0m },
        { 1, 1m, -0.01m },
        { 1, 1m, 100.01m },
        { 1, 1m, 0.001m }
    };

    [Theory, MemberData(nameof(Invalidos))]
    public void Rechaza_cantidad_precio_o_descuento_invalido(int cantidad, decimal precio, decimal descuento)
    {
        var error = Assert.Throws<ApiException>(() => ImportesMovimiento.Validar([(cantidad, precio, descuento)], "Límite total"));
        Assert.Equal(400, error.StatusCode);
        Assert.Equal("Un detalle contiene cantidades, precios o descuentos inválidos.", error.Message);
    }

    [Fact]
    public void Limita_subtotal_y_acumulado_y_conserva_el_mensaje_del_flujo()
    {
        foreach (var mensaje in new[] { "El total del ingreso excede el rango decimal(18,2).", "El total de la salida excede el rango decimal(18,2)." })
        {
            ImportesMovimiento.Validar([(1, Max, 0m)], mensaje);
            ImportesMovimiento.Validar([(int.MaxValue, Max, 100m)], mensaje);
            foreach (var detalles in new (int, decimal, decimal)[][]
            {
                [(2, Max, 0m)],
                [(1, Max, 0m), (1, 0.01m, 0m)]
            })
            {
                var error = Assert.Throws<ApiException>(() => ImportesMovimiento.Validar(detalles, mensaje));
                Assert.Equal(400, error.StatusCode);
                Assert.Equal(mensaje, error.Message);
            }
        }
    }
}
