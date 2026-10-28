using System.Net;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Controllers;
using TPN_5_MOVIL.Entities;
using UglyToad.PdfPig;
using Xunit;

namespace TPN5.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class ReportesIntegrationTests(SqlServerFixture sql)
{
    private static async Task<string> PdfAsync(TestApplication app, string path)
    {
        using var response = await app.SendAsync(HttpMethod.Get, path, app.Admin);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.EndsWith(".pdf", response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        using var document = PdfDocument.Open(await response.Content.ReadAsByteArrayAsync());
        Assert.True(document.NumberOfPages > 0);
        return ReportesPdfTests.Texto(document);
    }

    [Fact]
    public async Task Tres_reportes_solo_para_admin_y_vacios_validos_sin_datos_personales()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var worker = await app.RegisterAsync(Usuario.RolEmpleado);
        var client = await app.RegisterAsync(Usuario.RolCliente);
        foreach (var name in new[] { "stock", "ingresos", "salidas" })
        {
            var path = "/api/reportes/" + name;
            await app.StatusAsync(HttpMethod.Get, path, HttpStatusCode.Unauthorized);
            await app.StatusAsync(HttpMethod.Get, path, HttpStatusCode.Forbidden, worker);
            await app.StatusAsync(HttpMethod.Get, path, HttpStatusCode.Forbidden, client);
            var text = await PdfAsync(app, path);
            Assert.Contains("Sin datos para los filtros seleccionados", text);
            Assert.DoesNotContain(app.Password, text);
            Assert.DoesNotContain(app.Admin.Email, text);
        }
        foreach (var name in new[] { "ingresos", "salidas" })
        {
            foreach (var filter in new[] { "desde=2026-10-28&hasta=2026-10-27", "desde=invalido", "hasta=9999-12-31", "hasta=2026-02-30" })
                await app.StatusAsync(HttpMethod.Get, $"/api/reportes/{name}?{filter}", HttpStatusCode.BadRequest, app.Admin);
            Assert.Contains("Sin datos", await PdfAsync(app, $"/api/reportes/{name}?desde=2026-10-01&hasta=2026-10-27"));
        }
    }

    [Fact]
    public async Task Stock_activo_incluye_cero_y_categoria_eliminada_sin_modificar_datos()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        await app.DatabaseAsync(async db =>
        {
            var category = new Categoria { Nombre = "CATEGORIA_HISTORICA", Eliminado = true };
            db.AddRange(new Producto { Nombre = "AGOTADO_VISIBLE", Categoria = category, Stock = 0, Precio = 1234.56m },
                new Producto { Nombre = "DISPONIBLE_VISIBLE", Categoria = category, Stock = 5, Precio = 5 },
                new Producto { Nombre = "ELIMINADO_OCULTO", Categoria = category, Stock = 9, Precio = 10, Eliminado = true });
            await db.SaveChangesAsync();
        });
        var text = await PdfAsync(app, "/api/reportes/stock");
        Assert.Contains("AGOTADO_VISIBLE", text);
        Assert.Contains("DISPONIBLE_VISIBLE", text);
        Assert.Contains("CATEGORIA_HISTORICA", text);
        Assert.Contains("1.234,56", text);
        Assert.Contains("Cantidad de productos: 2", text);
        Assert.DoesNotContain("ELIMINADO_OCULTO", text);
        await app.DatabaseAsync(async db =>
        {
            Assert.Equal(3, await db.Productos.IgnoreQueryFilters().CountAsync());
            Assert.Equal(14, await db.Productos.IgnoreQueryFilters().SumAsync(p => p.Stock));
            Assert.Single(await db.Productos.IgnoreQueryFilters().Where(p => p.Eliminado).ToListAsync());
            Assert.True((await db.Categorias.IgnoreQueryFilters().SingleAsync()).Eliminado);
        });
    }

    [Fact]
    public async Task Movimientos_confirmados_fechas_limites_historicos_y_redondeo_por_detalle()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var worker = await app.RegisterAsync(Usuario.RolEmpleado);
        var client = await app.RegisterAsync(Usuario.RolCliente);
        var date = new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc);
        await app.DatabaseAsync(async db =>
        {
            var category = new Categoria { Nombre = "Categoria", Eliminado = true };
            var p1 = new Producto { Nombre = "Histórico1", Categoria = category, Stock = 12, Precio = 999, Eliminado = true };
            var p2 = new Producto { Nombre = "Histórico2", Categoria = category, Stock = 7, Precio = 888, Eliminado = true };
            var sequence = 0;
            void Income(string token, DateTime? confirmation, bool deleted = false)
            {
                var provider = new Proveedor { Nombre = token, CUIT = (++sequence).ToString(), Eliminado = true };
                db.Ingresos.Add(new Ingreso { Fecha = date.AddYears(-1), Proveedor = provider, EmpleadoId = worker.Id,
                    Confirmado = confirmation.HasValue, FechaConfirmacion = confirmation, EmpleadoConfirmacionId = confirmation.HasValue ? worker.Id : null,
                    Eliminado = deleted, Detalles = [new() { Producto = p1, Cantidad = 1, PrecioUnitario = .01m, DescAplicado = 50 }, new() { Producto = p2, Cantidad = 1, PrecioUnitario = .01m, DescAplicado = 50 }] });
            }
            void Sale(string token, DateTime? confirmation, bool requested, bool deleted = false, bool byAdmin = false)
            {
                var owner = new Usuario { Nombre = token, Apellido = "Cliente", CUIT = (1000 + ++sequence).ToString(), Email = $"history{sequence}@example.test", PasswordHash = "NotUsedForLogin", Rol = Usuario.RolCliente, Eliminado = true };
                var profile = new Cliente { Usuario = owner };
                db.Salidas.Add(new Salida { Fecha = date.AddYears(-1), Cliente = profile, ConfirmarXCliente = requested,
                    FechaConfirmacionCliente = requested ? date.AddHours(-1) : null, ConfirmarXEmpleado = confirmation.HasValue,
                    FechaConfirmacionEmpleado = confirmation, UsuarioConfirmacionId = confirmation.HasValue ? byAdmin ? app.Admin.Id : worker.Id : null,
                    Eliminado = deleted, Detalles = [new() { Producto = p1, Cantidad = 1, PrecioUnitario = .01m, DescAplicado = 50 }, new() { Producto = p2, Cantidad = 1, PrecioUnitario = .01m, DescAplicado = 50 }] });
            }
            Income("I_INICIO", date); Income("I_FIN", date.AddDays(1).AddMilliseconds(-1));
            Income("I_ANTES", date.AddMilliseconds(-1)); Income("I_DESPUES", date.AddDays(1));
            Income("I_BORRADOR", null); Income("I_REVERTIDO", date, true);
            Sale("S_INICIO", date, true); Sale("S_FIN", date.AddDays(1).AddMilliseconds(-1), true, byAdmin: true);
            Sale("S_ANTES", date.AddMilliseconds(-1), true); Sale("S_DESPUES", date.AddDays(1), true);
            Sale("S_BORRADOR", null, false); Sale("S_PENDIENTE", null, true); Sale("S_CANCELADO", date, true, true);
            await db.SaveChangesAsync();
            await db.Usuarios.Where(u => u.Id == worker.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Eliminado, true).SetProperty(u => u.Nombre, "CONFIRMADOR_HISTORICO"));
        });
        var incoming = await PdfAsync(app, "/api/reportes/ingresos?desde=2026-10-01&hasta=2026-10-01");
        var outgoing = await PdfAsync(app, "/api/reportes/salidas?desde=2026-10-01&hasta=2026-10-01");
        foreach (var text in new[] { incoming, outgoing })
        {
            Assert.Contains("Cantidad de movimientos: 2", text);
            Assert.Contains("CONFIRMADOR_HISTORICO", text);
            Assert.Contains("00:00:00", text);
            Assert.Contains("23:59:59", text);
            Assert.Contains("0,02", text); Assert.Contains("0,04", text);
            Assert.DoesNotContain("999,00", text);
            Assert.DoesNotContain("888,00", text);
        }
        Assert.Contains("I_INICIO", incoming); Assert.Contains("I_FIN", incoming);
        foreach (var token in new[] { "I_ANTES", "I_DESPUES", "I_BORRADOR", "I_REVERTIDO" }) Assert.DoesNotContain(token, incoming);
        Assert.Contains("S_INICIO", outgoing); Assert.Contains("S_FIN", outgoing); Assert.Contains("Admin Prueba", outgoing);
        foreach (var token in new[] { "S_ANTES", "S_DESPUES", "S_BORRADOR", "S_PENDIENTE", "S_CANCELADO" }) Assert.DoesNotContain(token, outgoing);
        Assert.Contains("Cantidad de movimientos: 4", await PdfAsync(app, "/api/reportes/ingresos"));
        Assert.Contains("Cantidad de movimientos: 4", await PdfAsync(app, "/api/reportes/salidas"));
        await app.DatabaseAsync(async db =>
        {
            Assert.Equal(19, await db.Productos.IgnoreQueryFilters().SumAsync(p => p.Stock));
            Assert.Equal(6, await db.Ingresos.IgnoreQueryFilters().CountAsync());
            Assert.Equal(7, await db.Salidas.IgnoreQueryFilters().CountAsync());
            Assert.Equal(26, await db.DetallesIngreso.IgnoreQueryFilters().CountAsync() + await db.DetallesSalida.IgnoreQueryFilters().CountAsync());
        });
    }

    [Fact]
    public async Task Limites_de_filas_y_detalles_rechazan_exceso_sin_truncar_y_suma_supera_un_movimiento()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var worker = await app.RegisterAsync(Usuario.RolEmpleado);
        var client = await app.RegisterAsync(Usuario.RolCliente);
        var day = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        await app.DatabaseAsync(async db =>
        {
            var category = new Categoria { Nombre = "Limites" };
            var products = Enumerable.Range(0, ReportesController.MaxDetalles + 1).Select(i => new Producto { Nombre = $"Producto{i:D4}", Categoria = category, Precio = 1, Stock = 1 }).ToArray();
            var supplier = new Proveedor { Nombre = "Limites", CUIT = "991" };
            db.AddRange(products);
            for (var i = 0; i <= ReportesController.MaxFilas; i++)
            {
                db.Ingresos.Add(new Ingreso { Proveedor = supplier, EmpleadoId = worker.Id, Confirmado = true, FechaConfirmacion = day, EmpleadoConfirmacionId = worker.Id });
                db.Salidas.Add(new Salida { ClienteId = client.Id, ConfirmarXCliente = true, FechaConfirmacionCliente = day, ConfirmarXEmpleado = true, FechaConfirmacionEmpleado = day, UsuarioConfirmacionId = app.Admin.Id });
            }
            db.Ingresos.Add(new Ingreso { Proveedor = supplier, EmpleadoId = worker.Id, Confirmado = true, FechaConfirmacion = day.AddDays(1), EmpleadoConfirmacionId = worker.Id,
                Detalles = products.Select(p => new DetalleIngreso { Producto = p, Cantidad = 1, PrecioUnitario = 1 }).ToList() });
            db.Salidas.Add(new Salida { ClienteId = client.Id, ConfirmarXCliente = true, FechaConfirmacionCliente = day, ConfirmarXEmpleado = true, FechaConfirmacionEmpleado = day.AddDays(1), UsuarioConfirmacionId = app.Admin.Id,
                Detalles = products.Select(p => new DetalleSalida { Producto = p, Cantidad = 1, PrecioUnitario = 1 }).ToList() });
            // Two independently valid historical movement totals whose report sum exceeds decimal(18,2).
            for (var i = 0; i < 2; i++)
            {
                db.Ingresos.Add(new Ingreso { Proveedor = supplier, EmpleadoId = worker.Id, Confirmado = true, FechaConfirmacion = day.AddDays(2), EmpleadoConfirmacionId = worker.Id,
                    Detalles = [new() { Producto = products[0], Cantidad = 1, PrecioUnitario = 9999999999999999.99m }] });
                db.Salidas.Add(new Salida { ClienteId = client.Id, ConfirmarXCliente = true, FechaConfirmacionCliente = day, ConfirmarXEmpleado = true, FechaConfirmacionEmpleado = day.AddDays(2), UsuarioConfirmacionId = app.Admin.Id,
                    Detalles = [new() { Producto = products[0], Cantidad = 1, PrecioUnitario = 9999999999999999.99m }] });
            }
            await db.SaveChangesAsync();
        });
        using (var response = await app.SendAsync(HttpMethod.Get, "/api/reportes/stock", app.Admin))
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("No se generó un reporte parcial", await response.Content.ReadAsStringAsync());
        }
        foreach (var name in new[] { "ingresos", "salidas" })
        {
            foreach (var date in new[] { "2026-10-01", "2026-10-02" })
            {
                using var rejected = await app.SendAsync(HttpMethod.Get, $"/api/reportes/{name}?desde={date}&hasta={date}", app.Admin);
                Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
                Assert.Contains("No se generó un reporte parcial", await rejected.Content.ReadAsStringAsync());
            }
            Assert.Contains("19.999.999.999.999.999,98", await PdfAsync(app, $"/api/reportes/{name}?desde=2026-10-03&hasta=2026-10-03"));
        }
    }
}

