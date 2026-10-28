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

