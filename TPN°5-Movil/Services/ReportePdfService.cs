using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TPN_5_MOVIL.DTOs;

namespace TPN_5_MOVIL.Services;

// Renders supplied data only. Database queries and authorization belong to the controller.
public sealed class ReportePdfService : IReportePdfService
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");
    private const string Azul = "#243B53";
    private const string Borde = "#D9E2EC";
    private const string Alterno = "#F0F4F8";

    public byte[] GenerarStock(IReadOnlyCollection<ReporteStockFila> filas, ReporteContexto contexto) =>
        Crear("Stock actual", "Productos activos; incluye stock cero", filas.Count, contexto, column =>
        {
            if (filas.Count == 0) { SinDatos(column); return; }
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.ConstantColumn(55); c.RelativeColumn(2); c.RelativeColumn(1.5f); c.ConstantColumn(125); c.ConstantColumn(70); });
                table.Header(h =>
                {
                    Encabezado(h.Cell(), "ID"); Encabezado(h.Cell(), "Producto"); Encabezado(h.Cell(), "Categoría");
                    Encabezado(h.Cell(), "Precio", true); Encabezado(h.Cell(), "Stock", true);
                });
                var index = 0;
                foreach (var row in filas)
                {
                    var color = index++ % 2 == 0 ? "#FFFFFF" : Alterno;
                    Celda(table.Cell(), row.Id.ToString(Cultura), color);
                    Celda(table.Cell(), row.Nombre, color); Celda(table.Cell(), row.Categoria, color);
                    Celda(table.Cell(), Moneda(row.Precio), color, true);
                    Celda(table.Cell(), row.Stock.ToString("N0", Cultura), color, true);
                }
            });
            column.Item().PaddingTop(10).AlignRight().Text($"Unidades en stock: {filas.Sum(r => (long)r.Stock).ToString("N0", Cultura)}").SemiBold();
        });

    public byte[] GenerarIngresos(IReadOnlyCollection<ReporteMovimientoFila> filas, ReporteContexto contexto) =>
        Movimientos("Entradas de mercadería confirmadas", "Proveedor", filas, contexto);
    public byte[] GenerarSalidas(IReadOnlyCollection<ReporteMovimientoFila> filas, ReporteContexto contexto) =>
        Movimientos("Ventas confirmadas", "Cliente", filas, contexto);

    private static byte[] Movimientos(string titulo, string contraparte, IReadOnlyCollection<ReporteMovimientoFila> filas, ReporteContexto contexto) =>
        Crear(titulo, Filtros(contexto), filas.Count, contexto, column =>
        {
            if (filas.Count == 0) SinDatos(column);
            else column.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.ConstantColumn(55); c.ConstantColumn(84); c.RelativeColumn(); c.RelativeColumn(); c.ConstantColumn(130); });
                table.Header(h =>
                {
                    Encabezado(h.Cell(), "ID"); Encabezado(h.Cell(), "Confirmación"); Encabezado(h.Cell(), contraparte);
                    Encabezado(h.Cell(), "Confirmado por"); Encabezado(h.Cell(), "Total", true);
                });
                var index = 0;
                foreach (var row in filas)
                {
                    var color = index++ % 2 == 0 ? "#FFFFFF" : Alterno;
                    Celda(table.Cell(), row.Id.ToString(Cultura), color);
                    Celda(table.Cell(), ReporteContexto.Presentar(row.FechaConfirmacionUtc).ToString("dd/MM/yyyy HH:mm:ss", Cultura), color);
                    Celda(table.Cell(), row.Contraparte, color); Celda(table.Cell(), row.Confirmador, color);
                    Celda(table.Cell(), Moneda(row.Total), color, true);
                }
            });
            // Do not impose a single movement's decimal(18,2) limit on this accumulated total.
            column.Item().PaddingTop(10).AlignRight().Text($"Total del reporte: {Moneda(filas.Sum(r => r.Total))}").SemiBold();
        });

    private static byte[] Crear(string titulo, string filtros, int cantidad, ReporteContexto contexto, Action<ColumnDescriptor> contenido) =>
        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.DefaultTextStyle(s => s.FontSize(9));
            page.Header().Column(c =>
            {
                c.Item().Text("APINVENTARIO").FontSize(10).SemiBold().FontColor(Azul);
                c.Item().PaddingTop(4).Text(titulo).FontSize(18).Bold().FontColor(Azul);
                c.Item().PaddingTop(5).Text($"Generado: {ReporteContexto.Presentar(contexto.GeneradoUtc.UtcDateTime).ToString("dd/MM/yyyy HH:mm:ss", Cultura)} | Cantidad de {(titulo == "Stock actual" ? "productos" : "movimientos")}: {cantidad.ToString("N0", Cultura)}").FontSize(9);
                c.Item().PaddingTop(3).Text("Fechas de presentación: Buenos Aires, UTC-03:00 (offset fijo).").FontSize(8).FontColor(Colors.Grey.Darken2);
                c.Item().PaddingTop(3).Text($"Filtros: {filtros}").FontSize(8);
                c.Item().PaddingTop(8).Height(2).Background(Azul);
            });
            page.Content().PaddingVertical(12).Column(contenido);
            page.Footer().AlignRight().Text(text =>
            {
                text.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken2));
                text.Span("Página "); text.CurrentPageNumber(); text.Span(" de "); text.TotalPages();
            });
        })).GeneratePdf();

    private static string Filtros(ReporteContexto contexto) =>
        $"Confirmados vigentes | Desde: {(contexto.Desde.HasValue ? contexto.Desde.Value.ToString("dd/MM/yyyy", Cultura) : "sin límite")} | Hasta: {(contexto.Hasta.HasValue ? contexto.Hasta.Value.ToString("dd/MM/yyyy", Cultura) + " (día completo)" : "sin límite")}";
    private static string Moneda(decimal value) => value.ToString("C2", Cultura);
    private static void SinDatos(ColumnDescriptor column) =>
        column.Item().PaddingVertical(20).Text("Sin datos para los filtros seleccionados").FontColor(Colors.Grey.Darken2);
    private static void Encabezado(IContainer container, string value, bool derecha = false)
    {
        var cell = container.Background(Azul).Padding(5);
        (derecha ? cell.AlignRight() : cell).Text(value).FontColor(Colors.White).SemiBold().FontSize(8);
    }
    private static void Celda(IContainer container, string value, string color, bool derecha = false)
    {
        var cell = container.ShowEntire().Background(color).BorderBottom(.5f).BorderColor(Borde).Padding(5);
        (derecha ? cell.AlignRight() : cell).Text(value).FontSize(8);
    }
}

