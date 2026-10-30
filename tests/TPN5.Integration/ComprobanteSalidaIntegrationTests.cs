using System.Net;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using UglyToad.PdfPig;
using Xunit;

namespace TPN5.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class ComprobanteSalidaIntegrationTests(SqlServerFixture sql)
{
    private static async Task<string> PdfAsync(TestApplication app, string path, TestAccount actor)
    {
        using var response = await app.SendAsync(HttpMethod.Get, path, actor);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.EndsWith(".pdf", response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        using var document = PdfDocument.Open(await response.Content.ReadAsByteArrayAsync());
        return ReportesPdfTests.Texto(document);
    }

    [Fact]
    public async Task Comprobante_persiste_medio_pago_y_respeta_estado_y_propietario()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var client = await app.RegisterAsync(Usuario.RolCliente);
        var other = await app.RegisterAsync(Usuario.RolCliente);
        var employee = await app.RegisterAsync(Usuario.RolEmpleado);

        var saleId = 0;
        var noMethodSaleId = 0;
        var cancelledSaleId = 0;
        await app.DatabaseAsync(async db =>
        {
            var category = new Categoria { Nombre = "Comprobante" };
            var product = new Producto { Nombre = "Producto comprobante", Categoria = category, Precio = 100m, Stock = 20 };
            var sale = new Salida
            {
                ClienteId = client.Id,
                Fecha = new DateTime(2026, 10, 27, 15, 45, 0, DateTimeKind.Utc),
                Observaciones = "Retirar por la tarde",
                Detalles = [new() { Producto = product, Cantidad = 2, PrecioUnitario = 100m, DescAplicado = 10m }]
            };
            var noMethodSale = new Salida
            {
                ClienteId = client.Id,
                Detalles = [new() { Producto = product, Cantidad = 1, PrecioUnitario = 100m }]
            };
            var cancelled = new Salida
            {
                ClienteId = client.Id,
                ConfirmarXCliente = true,
                FechaConfirmacionCliente = DateTime.UtcNow,
                Eliminado = true,
                Detalles = [new() { Producto = product, Cantidad = 1, PrecioUnitario = 100m }]
            };
            db.AddRange(sale, noMethodSale, cancelled);
            await db.SaveChangesAsync();
            saleId = sale.Id;
            noMethodSaleId = noMethodSale.Id;
            cancelledSaleId = cancelled.Id;
        });

        var path = $"/api/salidas/{saleId}/comprobante";
        await app.StatusAsync(HttpMethod.Get, path, HttpStatusCode.Unauthorized);
        await app.StatusAsync(HttpMethod.Get, path, HttpStatusCode.NotFound, other);
        await app.StatusAsync(HttpMethod.Get, path, HttpStatusCode.Conflict, client);
        await app.StatusAsync(HttpMethod.Post, $"/api/salidas/{saleId}/confirmar-cliente",
            HttpStatusCode.BadRequest, client, new { medioPago = "Bitcoin" });
        await app.StatusAsync(HttpMethod.Get, $"/api/salidas/{cancelledSaleId}/comprobante",
            HttpStatusCode.Conflict, client);

        var confirmed = await app.JsonAsync<SalidaResponse>(HttpMethod.Post,
            $"/api/salidas/{saleId}/confirmar-cliente", client, new { medioPago = "tarjeta" });
        Assert.Equal("Tarjeta", confirmed.MedioPago);
        Assert.True(confirmed.ConfirmarXCliente);
        Assert.Equal("Tarjeta",
            (await app.JsonAsync<SalidaResponse>(HttpMethod.Get, $"/api/salidas/{saleId}", client)).MedioPago);

        var pending = await PdfAsync(app, path, client);
        Assert.Contains("DOCUMENTO NO FISCAL", pending);
        Assert.Contains("PENDIENTE DE ACEPTACIÓN", pending);
        Assert.Contains("Tarjeta (simulado)", pending);
        Assert.Contains("Producto comprobante", pending);
        Assert.Contains("Retirar por la tarde", pending);
        Assert.Contains("180,00", pending);
        await app.StatusAsync(HttpMethod.Get, path, HttpStatusCode.OK, employee);
        await app.StatusAsync(HttpMethod.Get, path, HttpStatusCode.OK, app.Admin);

        var accepted = await app.JsonAsync<SalidaResponse>(HttpMethod.Post,
            $"/api/salidas/{saleId}/confirmar", employee);
        Assert.True(accepted.ConfirmarXEmpleado);
        var acceptedPdf = await PdfAsync(app, path, client);
        Assert.Contains("ACEPTADO", acceptedPdf);
        Assert.Contains("Aceptado por", acceptedPdf);
        Assert.DoesNotContain("PENDIENTE DE ACEPTACIÓN", acceptedPdf);

        var withoutMethod = await app.JsonAsync<SalidaResponse>(HttpMethod.Post,
            $"/api/salidas/{noMethodSaleId}/confirmar-cliente", client);
        Assert.True(withoutMethod.ConfirmarXCliente);
        Assert.Null(withoutMethod.MedioPago);
        Assert.Contains("No informado",
            await PdfAsync(app, $"/api/salidas/{noMethodSaleId}/comprobante", client));
    }
}
