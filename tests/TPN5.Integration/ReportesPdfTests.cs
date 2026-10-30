using System.ComponentModel.DataAnnotations;
using System.Globalization;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Services;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Xunit;

namespace TPN5.Integration;

public sealed class ReportesPdfTests
{
    private static readonly ReporteContexto Contexto = new(new DateTimeOffset(2026, 10, 27, 15, 45, 0, TimeSpan.Zero), new(2026, 10, 1), new(2026, 10, 27));
    private static ReportePdfService Servicio()
    {
        // The owner confirmed exclusively academic use for this project.
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        return new();
    }
    internal static string Texto(PdfDocument document) => string.Join("\n", document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page)));

    private static ReporteComprobanteSalida Comprobante(bool aceptado = false, string? medioPago = "Tarjeta")
    {
        var fecha = new DateTime(2026, 10, 27, 15, 45, 0, DateTimeKind.Utc);
        return new(42, fecha, "Ana", "Pérez", "20-12345678-9", "ana@example.test", "11-5555-5555",
            "Calle Falsa 123", "Entregar por la tarde", medioPago, true, aceptado, fecha.AddMinutes(5),
            aceptado ? fecha.AddMinutes(10) : null, aceptado ? "Luis" : null, aceptado ? "Gómez" : null,
            [new("Yerba mate", 2, 1000m, 10m, 1800m), new("Café molido", 1, 500m, 0m, 500m)], 2300m);
    }

    [Theory]
    [InlineData("stock")]
    [InlineData("ingresos")]
    [InlineData("salidas")]
    public void Vacio_es_un_documento_A4_con_mensaje_y_numero_de_pagina(string tipo)
    {
        var service = Servicio();
        var bytes = tipo switch { "stock" => service.GenerarStock([], Contexto), "ingresos" => service.GenerarIngresos([], Contexto), _ => service.GenerarSalidas([], Contexto) };
        using var document = PdfDocument.Open(bytes);
        Assert.Equal(1, document.NumberOfPages);
        var page = document.GetPage(1);
        Assert.InRange((double)page.Width, 594, 596);
        Assert.InRange((double)page.Height, 841, 843);
        var text = Texto(document);
        Assert.Contains("Sin datos para los filtros seleccionados", text);
        Assert.Contains("27/10/2026 12:45:00", text);
        Assert.Contains("UTC-03:00", text);
        Assert.Contains("Página 1 de 1", text);
    }

    [Fact]
    public void Stock_incluye_agotados_categoria_precio_y_cantidad_sin_valorar_inventario()
    {
        var bytes = Servicio().GenerarStock([new(71, "Producto agotado", "Categoría histórica", 1234.56m, 0), new(72, "Producto disponible", "Otra", 10, int.MaxValue)], Contexto);
        using var document = PdfDocument.Open(bytes);
        var text = Texto(document);
        Assert.Contains("Producto agotado", text);
        Assert.Contains("Categoría histórica", text);
        Assert.Contains("1.234,56", text);
        Assert.Contains("2.147.483.647", text);
        Assert.Contains("Cantidad de productos: 2", text);
        Assert.DoesNotContain("Valorizado", text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Totales_acumulados_superan_decimal_18_2_sin_truncar_y_fechas_no_dependen_del_servidor(bool ingreso)
    {
        var rows = new ReporteMovimientoFila[]
        {
            new(21, new DateTime(2026,10,27,2,59,59,DateTimeKind.Unspecified), "Referencia histórica", "Confirmador", 9999999999999999.99m),
            new(22, new DateTime(2026,10,27,3,0,0,DateTimeKind.Utc), "Otra referencia", "Otra persona", 9999999999999999.99m)
        };
        var service = Servicio();
        var bytes = ingreso ? service.GenerarIngresos(rows, Contexto) : service.GenerarSalidas(rows, Contexto);
        using var document = PdfDocument.Open(bytes);
        var text = Texto(document);
        Assert.Contains("19.999.999.999.999.999,98", text);
        Assert.Contains("26/10/2026", text);
        Assert.Contains("23:59:59", text);
        Assert.Contains("27/10/2026", text);
        Assert.Contains("00:00:00", text);
        Assert.Contains("Confirmador", text);
        Assert.Contains("01/10/2026", text);
        Assert.Contains("día completo", text);
    }

    [Theory]
    [InlineData("stock")]
    [InlineData("ingresos")]
    [InlineData("salidas")]
    public void Multipagina_con_encabezados_repetidos_texto_completo_y_paginado(string tipo)
    {
        var service = Servicio();
        var stock = Enumerable.Range(1, 160).Select(i => new ReporteStockFila(i, $"Artículo {i:D3} - " + new string('N', 120) + $"-FinN{i:D3}", "Categoría " + new string('C', 75) + $"-FinC{i:D3}", 123456.78m, int.MaxValue)).ToArray();
        var movements = Enumerable.Range(1, 160).Select(i => new ReporteMovimientoFila(i, new DateTime(2026,10,27,15,0,0,DateTimeKind.Utc), $"Contraparte {i:D3} - " + new string('P', 110) + $"-FinP{i:D3}", "Persona " + new string('A', 120) + $"-FinA{i:D3}", 123456.78m)).ToArray();
        var bytes = tipo switch { "stock" => service.GenerarStock(stock, Contexto), "ingresos" => service.GenerarIngresos(movements, Contexto), _ => service.GenerarSalidas(movements, Contexto) };
        using var document = PdfDocument.Open(bytes);
        Assert.True(document.NumberOfPages > 1);
        foreach (var page in document.GetPages())
        {
            var text = ContentOrderTextExtractor.GetText(page);
            Assert.Contains("APINVENTARIO", text);
            Assert.Contains($"Página {page.Number} de {document.NumberOfPages}", text);
            // Footer and all visible text stay within the A4 page.
            Assert.All(page.Letters, letter =>
            {
                Assert.InRange(letter.BoundingBox.Left, 0, (double)page.Width);
                Assert.InRange(letter.BoundingBox.Bottom, 0, (double)page.Height);
            });
        }
        var pageTexts = document.GetPages().Select(p => System.Text.RegularExpressions.Regex.Replace(ContentOrderTextExtractor.GetText(p), @"\s", "")).ToArray();
        for (var i = 1; i <= 160; i++)
        {
            var rowPage = Assert.Single(pageTexts, t => t.Contains(tipo == "stock" ? $"Artículo{i:D3}" : $"Contraparte{i:D3}"));
            Assert.Contains(tipo == "stock" ? $"FinN{i:D3}" : $"FinP{i:D3}", rowPage);
            Assert.Contains(tipo == "stock" ? $"FinC{i:D3}" : $"FinA{i:D3}", rowPage);
        }
        var all = Texto(document);
        Assert.Contains(tipo == "stock" ? "Artículo 001" : "Contraparte 001", all);
        Assert.Contains(tipo == "stock" ? "Artículo 160" : "Contraparte 160", all);
        Assert.Contains(tipo == "stock" ? "343.597.383.520" : "19.753.084,80", all);
        // Optional external evidence only; the API never saves PDFs.
        if (Environment.GetEnvironmentVariable("TPN5_PDF_REVIEW_DIR") is { Length: > 0 } output)
        {
            Directory.CreateDirectory(output);
            File.WriteAllBytes(Path.Combine(output, tipo + ".pdf"), bytes);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Comprobante_incluye_datos_pago_estado_y_leyenda_no_fiscal(bool aceptado)
    {
        var bytes = Servicio().GenerarComprobante(Comprobante(aceptado), Contexto);
        using var document = PdfDocument.Open(bytes);
        var text = Texto(document);
        Assert.Contains("APINVENTARIO", text);
        Assert.Contains("Comprobante de compra", text);
        Assert.Contains("Pedido #000042", text);
        Assert.Contains("DOCUMENTO NO FISCAL", text);
        Assert.Contains("Documento no fiscal", text);
        Assert.Contains("Ana", text);
        Assert.Contains("Pérez", text);
        Assert.Contains("20-12345678-9", text);
        Assert.Contains("ana@example.test", text);
        Assert.Contains("Tarjeta (simulado)", text);
        Assert.Contains("Yerba mate", text);
        Assert.Contains("Café molido", text);
        Assert.Contains("2.300,00", text);
        Assert.Contains("200,00", text);
        Assert.Contains("Entregar por la tarde", text);
        Assert.Contains("Página 1 de 1", text);
        if (aceptado)
        {
            Assert.Contains("ACEPTADO", text);
            Assert.Contains("Luis", text);
        }
        else
        {
            Assert.Contains("PENDIENTE DE ACEPTACIÓN", text);
            Assert.Contains("stock se descuenta", text);
        }
    }

    [Fact]
    public void Comprobante_sin_medio_de_pago_es_valido()
    {
        var bytes = Servicio().GenerarComprobante(Comprobante(medioPago: null), Contexto);
        using var document = PdfDocument.Open(bytes);
        Assert.Contains("No informado", Texto(document));
    }

    [Fact]
    public void Comprobante_multipagina_repite_encabezados_y_mantiene_todo_visible()
    {
        var details = Enumerable.Range(1, 120)
            .Select(i => new ReporteDetalleComprobante($"Producto {i:D3} - " + new string('P', 100) + $"-Fin{i:D3}",
                1, 100m, 0m, 100m)).ToArray();
        var comprobante = Comprobante() with { Detalles = details, Total = details.Sum(d => d.Subtotal) };
        var bytes = Servicio().GenerarComprobante(comprobante, Contexto);
        using var document = PdfDocument.Open(bytes);
        Assert.True(document.NumberOfPages > 1);
        foreach (var page in document.GetPages())
        {
            var text = ContentOrderTextExtractor.GetText(page);
            Assert.Contains("APINVENTARIO", text);
            Assert.Contains("DOCUMENTO NO FISCAL", text);
            Assert.Contains($"Página {page.Number} de {document.NumberOfPages}", text);
            Assert.All(page.Letters, letter =>
            {
                Assert.InRange(letter.BoundingBox.Left, 0, (double)page.Width);
                Assert.InRange(letter.BoundingBox.Bottom, 0, (double)page.Height);
            });
        }
    }

    [Fact]
    public void Fechas_del_reporte_incluyen_hasta_completo_y_validan_extremos()
    {
        var filter = new ReporteFechasFiltro { Desde = new(2026,10,1), Hasta = new(2026,10,27) };
        Assert.Equal(new DateTime(2026,10,1,3,0,0,DateTimeKind.Utc), filter.ObtenerDesdeUtc());
        Assert.Equal(new DateTime(2026,10,28,3,0,0,DateTimeKind.Utc), filter.ObtenerHastaExclusivoUtc());
        foreach (var invalid in new[] { new ReporteFechasFiltro { Desde = new(2026,10,28), Hasta = new(2026,10,27) }, new ReporteFechasFiltro { Hasta = DateOnly.MaxValue } })
            Assert.False(Validator.TryValidateObject(invalid, new ValidationContext(invalid), [], true));
    }
}

