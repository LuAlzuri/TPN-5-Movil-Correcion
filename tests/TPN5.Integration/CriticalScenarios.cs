using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Security;
using Xunit;

namespace TPN5.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class CriticalScenarios(SqlServerFixture sql)
{
    [Fact]
    public async Task Registro_publico_crea_cliente_y_bloquea_roles_de_personal()
    {
        await using var app = await sql.CreateApplicationAsync();
        using var response = await app.SendAsync(HttpMethod.Post, "/api/auth/register", body: new
        {
            nombre = "Cliente", apellido = "Publico", cuit = "56789",
            email = "publico@example.test", password = app.Password,
            telefono = "123456", direccion = "Calle 123"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<UsuarioDto>())!;
        Assert.Equal(Usuario.RolCliente, created.Rol);
        var client = await app.LoginAsync(created.Email);
        await app.DatabaseAsync(async db =>
        {
            Assert.True(await db.Clientes.AnyAsync(c => c.Id == client.Id));
            Assert.False(await db.Empleados.AnyAsync(e => e.Id == client.Id));
        });

        await app.LoginAdminAsync();
        var employee = await app.RegisterAsync(Usuario.RolEmpleado);
        foreach (var actor in new TestAccount?[] { null, client, employee })
        foreach (var role in new[] { Usuario.RolEmpleado, Usuario.RolAdministrador })
            await app.StatusAsync(HttpMethod.Post, "/api/auth/register", HttpStatusCode.Forbidden, actor, new
            {
                nombre = "Personal", apellido = "Bloqueado", cuit = "67890",
                email = "bloqueado@example.test", password = app.Password,
                rol = role, fechaIngreso = role == Usuario.RolEmpleado ? "2026-01-01" : null
            });
        await app.RegisterAsync(Usuario.RolAdministrador);
        await app.DatabaseAsync(async db =>
            Assert.False(await db.Usuarios.AnyAsync(u => u.Email == "bloqueado@example.test")));
    }

    [Fact]
    public async Task Login_correcto_y_credenciales_invalidas_sin_enumerar_cuentas()
    {
        await using var app = await sql.CreateApplicationAsync();
        var admin = await app.LoginAdminAsync();
        var me = await app.JsonAsync<UsuarioDto>(HttpMethod.Get, "/api/auth/me", admin);
        Assert.Equal(admin.Id, me.Id);
        Assert.Equal(Usuario.RolAdministrador, me.Rol);
        using var wrong = await app.Http.PostAsJsonAsync("/api/auth/login", new { email = admin.Email, password = "wrong" });
        using var unknown = await app.Http.PostAsJsonAsync("/api/auth/login", new { email = "unknown@example.test", password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(wrong.StatusCode, unknown.StatusCode);
        Assert.Equal(await wrong.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        await app.StatusAsync(HttpMethod.Put, $"/api/usuarios/{admin.Id}", HttpStatusCode.NoContent, admin,
            new { nombre = " Admin ", apellido = " Prueba ", cuit = "1", email = " EDITED@EXAMPLE.TEST " });
        var updated = await app.LoginAsync("EDITED@EXAMPLE.TEST");
        var edited = await app.JsonAsync<UsuarioDto>(HttpMethod.Get, "/api/auth/me", updated);
        Assert.Equal(admin.Id, edited.Id);
        Assert.Equal("edited@example.test", edited.Email);
        Assert.Equal("Admin", edited.Nombre);

    }

    [Fact]
    public async Task Login_limita_solicitudes_y_recupera_cupo_sin_bloquear_otras_rutas()
    {
        var defaults = LoginRateLimitSettings.Load(new ConfigurationBuilder().Build());
        Assert.Equal(new LoginRateLimitSettings(5, 60), defaults);
        await using var app = await sql.CreateApplicationAsync(loginLimitScenario: true);
        var admin = await app.LoginAdminAsync(); // First of five requests.
        for (var i = 0; i < 4; i++)
            await app.StatusAsync(HttpMethod.Post, "/api/auth/login", HttpStatusCode.BadRequest,
                body: new { email = "invalid", password = "invalid" });
        using var blocked = await app.SendAsync(HttpMethod.Post, "/api/auth/login",
            body: new { email = admin.Email, password = app.Password });
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        var seconds = blocked.Headers.RetryAfter?.Delta?.TotalSeconds;
        Assert.NotNull(seconds);
        var wait = seconds.GetValueOrDefault();
        Assert.InRange(wait, 1, 5);
        var error = await blocked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(error.TryGetProperty("error", out _));
        await app.StatusAsync(HttpMethod.Get, "/api/auth/me", HttpStatusCode.OK, admin);
        // Honor server advice; the test window is five seconds, not a full minute.
        await Task.Delay(TimeSpan.FromSeconds(wait + 0.15));
        Assert.Equal(admin.Id, (await app.LoginAsync(admin.Email)).Id);
    }

    [Fact]
    public async Task Matriz_de_roles_y_rutas_anonimas()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var employee = await app.RegisterAsync(Usuario.RolEmpleado);
        var client = await app.RegisterAsync(Usuario.RolCliente);
        await app.StatusAsync(HttpMethod.Get, "/api/usuarios", HttpStatusCode.Unauthorized);
        await app.StatusAsync(HttpMethod.Get, "/api/usuarios", HttpStatusCode.Forbidden, client);
        await app.StatusAsync(HttpMethod.Post, "/api/usuarios", HttpStatusCode.Forbidden, employee, new { });
        await app.StatusAsync(HttpMethod.Get, "/api/auditoria/usuarios", HttpStatusCode.Forbidden, employee);
        await app.StatusAsync(HttpMethod.Get, "/api/proveedores", HttpStatusCode.Forbidden, client);
        await app.StatusAsync(HttpMethod.Get, "/api/categorias/catalogo", HttpStatusCode.OK, client);
    }

    [Fact]
    public async Task Registro_crea_perfiles_correctos_y_revierte_si_falla_el_perfil()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        // Fail insertion of Cliente after EF has attempted to insert its Usuario.
        await app.DatabaseAsync(db => db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE Clientes ADD CONSTRAINT CK_TestRejectClient CHECK (Id < 0);"));
        var request = new { nombre = "Cliente", apellido = "Prueba", cuit = "123", email = "rollback@example.test", password = app.Password, rol = "Cliente" };
        await app.StatusAsync(HttpMethod.Post, "/api/auth/register", HttpStatusCode.InternalServerError, app.Admin, request);
        await app.DatabaseAsync(async db =>
        {
            Assert.Equal(1, await db.Usuarios.CountAsync());
            Assert.Empty(await db.Clientes.ToListAsync());
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Clientes DROP CONSTRAINT CK_TestRejectClient;");
        });
        var client = await app.JsonAsync<UsuarioDto>(HttpMethod.Post, "/api/auth/register", app.Admin, request, HttpStatusCode.Created);
        var employee = await app.RegisterAsync(Usuario.RolEmpleado);
        await app.DatabaseAsync(async db =>
        {
            var c = await db.Usuarios.Include(u => u.Cliente).Include(u => u.Empleado).SingleAsync(u => u.Id == client.Id);
            Assert.NotNull(c.Cliente);
            Assert.Null(c.Empleado);
            Assert.Equal(c.Id, c.Cliente.Id);
            Assert.NotEqual(app.Password, c.PasswordHash);
            var e = await db.Empleados.SingleAsync(e => e.Id == employee.Id);
            Assert.Equal(employee.Id, e.Id);
        });
        await app.StatusAsync(HttpMethod.Put, $"/api/clientes/{client.Id}", HttpStatusCode.NoContent, app.Admin,
            new { nombre = " Cliente ", apellido = " Prueba ", cuit = "123", email = "CLIENT.EDITED@EXAMPLE.TEST", telefono = "   ", direccion = " Calle " });
        var editedClient = await app.JsonAsync<ClienteResponse>(HttpMethod.Get, $"/api/clientes/{client.Id}", app.Admin);
        Assert.Equal("client.edited@example.test", editedClient.Email);
        Assert.Equal("Cliente", editedClient.Nombre);
        Assert.Null(editedClient.Telefono);
        Assert.Equal("Calle", editedClient.Direccion);
        Assert.Equal(client.Id, (await app.LoginAsync("CLIENT.EDITED@EXAMPLE.TEST")).Id);
        await app.StatusAsync(HttpMethod.Put, $"/api/empleados/{employee.Id}", HttpStatusCode.NoContent, app.Admin,
            new { nombre = " Empleado ", apellido = " Prueba ", cuit = "456", email = "EMPLOYEE.EDITED@EXAMPLE.TEST", fechaIngreso = "2026-01-01" });
        var editedEmployee = await app.JsonAsync<EmpleadoResponse>(HttpMethod.Get, $"/api/empleados/{employee.Id}", app.Admin);
        Assert.Equal("employee.edited@example.test", editedEmployee.Email);
        Assert.Equal("Empleado", editedEmployee.Nombre);
        var employeeList = await app.JsonAsync<PaginatedResult<EmpleadoResponse>>(HttpMethod.Get,
            "/api/empleados?busqueda=EMPLOYEE.EDITED", app.Admin);
        Assert.Equal(editedEmployee, Assert.Single(employeeList.Items));
        Assert.Equal(employee.Id, (await app.LoginAsync("EMPLOYEE.EDITED@EXAMPLE.TEST")).Id);

    }

    [Fact]
    public async Task Ultimo_administrador_se_conserva_en_eliminaciones_simultaneas()
    {
        await using var app = await sql.CreateApplicationAsync();
        var first = await app.LoginAdminAsync();
        await app.StatusAsync(HttpMethod.Delete, $"/api/usuarios/{first.Id}", HttpStatusCode.Conflict, first);
        var second = await app.RegisterAsync(Usuario.RolAdministrador);
        var statuses = await RaceAsync(
            () => app.SendAsync(HttpMethod.Delete, $"/api/usuarios/{first.Id}", first),
            () => app.SendAsync(HttpMethod.Delete, $"/api/usuarios/{second.Id}", second));
        Assert.Single(statuses, s => s == HttpStatusCode.NoContent);
        Assert.Single(statuses, s => s == HttpStatusCode.Conflict);
        var survivor = statuses[0] == HttpStatusCode.NoContent ? second : first;
        await app.StatusAsync(HttpMethod.Get, "/api/auth/me", HttpStatusCode.OK, survivor);
        await app.StatusAsync(HttpMethod.Delete, $"/api/usuarios/{survivor.Id}", HttpStatusCode.Conflict, survivor);
        await app.DatabaseAsync(async db => Assert.Equal(1,
            await db.Usuarios.CountAsync(u => u.Rol == Usuario.RolAdministrador)));
    }

    [Fact]
    public async Task Soft_delete_oculta_recursos_conserva_perfiles_y_revoca_tokens()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var inventory = await InventoryAsync(app);
        await app.StatusAsync(HttpMethod.Delete, $"/api/productos/{inventory.ProductId}", HttpStatusCode.NoContent, inventory.Employee);
        await app.StatusAsync(HttpMethod.Get, $"/api/productos/{inventory.ProductId}", HttpStatusCode.NotFound, app.Admin);
        await app.StatusAsync(HttpMethod.Delete, $"/api/productos/{inventory.ProductId}", HttpStatusCode.NotFound, app.Admin);
        await app.StatusAsync(HttpMethod.Delete, $"/api/clientes/{inventory.Client.Id}", HttpStatusCode.NoContent, app.Admin);
        await app.StatusAsync(HttpMethod.Get, "/api/auth/me", HttpStatusCode.Unauthorized, inventory.Client);
        var audit = await app.JsonAsync<AuditoriaClienteResponse>(HttpMethod.Get,
            $"/api/auditoria/clientes/{inventory.Client.Id}", app.Admin);
        Assert.True(audit.Eliminado);
        await app.DatabaseAsync(async db => Assert.True(await db.Clientes.IgnoreQueryFilters().AnyAsync(c => c.Id == inventory.Client.Id)));
    }

    [Fact]
    public async Task Paginado_estable_y_parametros_invalidos()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        foreach (var name in new[] { "Alfa", "Beta", "Gamma" })
            await app.StatusAsync(HttpMethod.Post, "/api/categorias", HttpStatusCode.Created, app.Admin, new { nombre = name });
        var first = await app.JsonAsync<PaginatedResult<CategoriaResponse>>(HttpMethod.Get, "/api/categorias?pageSize=1", app.Admin);
        var repeat = await app.JsonAsync<PaginatedResult<CategoriaResponse>>(HttpMethod.Get, "/api/categorias?pageSize=1", app.Admin);
        var next = await app.JsonAsync<PaginatedResult<CategoriaResponse>>(HttpMethod.Get, "/api/categorias?page=2&pageSize=1", app.Admin);
        Assert.Single(first.Items);
        Assert.Equal(3, first.TotalCount);
        Assert.Equal(first.Items[0].Id, repeat.Items[0].Id);
        Assert.NotEqual(first.Items[0].Id, next.Items[0].Id);
        foreach (var path in new[] { "usuarios", "clientes", "empleados", "categorias", "proveedores", "productos", "productos/1/movimientos", "ingresos", "salidas", "auditoria/usuarios" })
            foreach (var query in new[] { "page=0", "pageSize=101", "page=2147483647&pageSize=100" })
                await app.StatusAsync(HttpMethod.Get, $"/api/{path}?{query}", HttpStatusCode.BadRequest, app.Admin);
    }

    [Fact]
    public async Task Producto_activo_conserva_categoria_eliminada_y_descripcion_normalizada()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var category = await app.JsonAsync<CategoriaResponse>(HttpMethod.Post, "/api/categorias", app.Admin,
            new { nombre = "Categoria historica" }, HttpStatusCode.Created);
        var product = await app.JsonAsync<ProductoResponse>(HttpMethod.Post, "/api/productos", app.Admin,
            new { nombre = "Producto historico", descripcion = "   ", precio = 12.35m, categoriaId = category.Id },
            HttpStatusCode.Created);
        Assert.Null(product.Descripcion);
        await app.StatusAsync(HttpMethod.Put, $"/api/productos/{product.Id}", HttpStatusCode.NoContent, app.Admin,
            new { nombre = product.Nombre, descripcion = " Detalle ", precio = product.Precio, categoriaId = category.Id });
        var before = await app.JsonAsync<ProductoResponse>(HttpMethod.Get, $"/api/productos/{product.Id}", app.Admin);
        Assert.Equal("Detalle", before.Descripcion);
        await app.StatusAsync(HttpMethod.Put, $"/api/productos/{product.Id}", HttpStatusCode.NoContent, app.Admin,
            new { nombre = product.Nombre, descripcion = "   ", precio = product.Precio, categoriaId = category.Id });
        var expected = before with { Descripcion = null };
        Assert.Equal(expected, await app.JsonAsync<ProductoResponse>(HttpMethod.Get, $"/api/productos/{product.Id}", app.Admin));
        await app.StatusAsync(HttpMethod.Delete, $"/api/categorias/{category.Id}", HttpStatusCode.NoContent, app.Admin);
        await app.StatusAsync(HttpMethod.Get, $"/api/categorias/{category.Id}", HttpStatusCode.NotFound, app.Admin);
        var detail = await app.JsonAsync<ProductoResponse>(HttpMethod.Get, $"/api/productos/{product.Id}", app.Admin);
        var list = await app.JsonAsync<PaginatedResult<ProductoResponse>>(HttpMethod.Get,
            $"/api/productos?categoriaId={category.Id}&busqueda=historico", app.Admin);
        Assert.Equal(expected, detail);
        Assert.Equal(category.Nombre, detail.CategoriaNombre);
        Assert.Equal(1, list.TotalCount);
        Assert.Equal(expected, Assert.Single(list.Items));
    }

    [Fact]
    public async Task Ingreso_confirma_stock_y_soft_delete_revierte_una_sola_vez()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var inventory = await InventoryAsync(app);
        var income = await IncomeAsync(app, inventory, [(inventory.ProductId, 10)], discount: 12.5m);
        Assert.Equal(0, await StockAsync(app, inventory.ProductId));
        Assert.Equal(108.06m, income.Total);
        income = await app.JsonAsync<IngresoResponse>(HttpMethod.Post, $"/api/ingresos/{income.Id}/confirmar", inventory.Employee);
        Assert.Equal(inventory.Employee.Id, income.EmpleadoConfirmacionId);
        Assert.Equal(10, await StockAsync(app, inventory.ProductId));
        await app.StatusAsync(HttpMethod.Post, $"/api/ingresos/{income.Id}/confirmar", HttpStatusCode.Conflict, inventory.Employee);
        await app.StatusAsync(HttpMethod.Put, $"/api/ingresos/{income.Id}/detalles/{income.Detalles[0].Id}",
            HttpStatusCode.Conflict, inventory.Employee, new { cantidad = 1 });
        await app.StatusAsync(HttpMethod.Delete, $"/api/ingresos/{income.Id}", HttpStatusCode.NoContent, inventory.Employee);
        Assert.Equal(0, await StockAsync(app, inventory.ProductId));
        await app.StatusAsync(HttpMethod.Delete, $"/api/ingresos/{income.Id}", HttpStatusCode.NotFound, inventory.Employee);
        var audit = await app.JsonAsync<AuditoriaIngresoResponse>(HttpMethod.Get, $"/api/auditoria/ingresos/{income.Id}", app.Admin);
        Assert.True(audit.Encabezado.Revertido);
        Assert.Single(audit.Detalles);
    }

    [Fact]
    public async Task Carrito_propio_detalle_unico_y_rechazo_de_acceso_ajeno()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var inventory = await InventoryAsync(app, 5);
        var other = await app.RegisterAsync(Usuario.RolCliente);
        var cart = await CartAsync(app, inventory.Client, [(inventory.ProductId, 1)]);
        var detail = await app.JsonAsync<DetalleSalidaResponse>(HttpMethod.Post, $"/api/salidas/{cart.Id}/detalles", inventory.Client,
            new { productoId = inventory.ProductId, cantidad = 3 });
        Assert.Equal(cart.Detalles[0].Id, detail.Id);
        Assert.Equal(3, detail.Cantidad);
        var current = await app.JsonAsync<SalidaResponse>(HttpMethod.Get, $"/api/salidas/{cart.Id}", inventory.Client);
        Assert.Single(current.Detalles);
        Assert.Equal(inventory.Client.Id, current.ClienteId);
        await app.StatusAsync(HttpMethod.Get, $"/api/salidas/{cart.Id}", HttpStatusCode.NotFound, other);
        await app.StatusAsync(HttpMethod.Put, $"/api/salidas/{cart.Id}/detalles/{detail.Id}", HttpStatusCode.NotFound, other, new { cantidad = 1 });
        await app.StatusAsync(HttpMethod.Delete, $"/api/salidas/{cart.Id}", HttpStatusCode.NotFound, other);
        await app.StatusAsync(HttpMethod.Post, "/api/salidas", HttpStatusCode.BadRequest, inventory.Client, new { clienteId = other.Id });
        var page = await app.JsonAsync<PaginatedResult<SalidaResponse>>(HttpMethod.Get,
            $"/api/salidas?clienteId={inventory.Client.Id}", other);
        Assert.Empty(page.Items);
        Assert.Equal(5, await StockAsync(app, inventory.ProductId));
    }

    [Fact]
    public async Task Cantidad_cero_retira_exceso_se_rechaza_y_venta_puede_agotar_stock()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var inventory = await InventoryAsync(app, 5);
        var cart = await CartAsync(app, inventory.Client, []);
        var path = $"/api/salidas/{cart.Id}/detalles";
        await app.StatusAsync(HttpMethod.Post, path, HttpStatusCode.Conflict, inventory.Client,
            new { productoId = inventory.ProductId, cantidad = 6 });
        var detail = await app.JsonAsync<DetalleSalidaResponse>(HttpMethod.Post, path, inventory.Client,
            new { productoId = inventory.ProductId, cantidad = 5 }, HttpStatusCode.Created);
        await app.StatusAsync(HttpMethod.Put, path + $"/{detail.Id}", HttpStatusCode.Conflict, inventory.Client, new { cantidad = 6 });
        await app.StatusAsync(HttpMethod.Put, path + $"/{detail.Id}", HttpStatusCode.BadRequest, inventory.Client, new { cantidad = -1 });
        await app.StatusAsync(HttpMethod.Put, path + $"/{detail.Id}", HttpStatusCode.NoContent, inventory.Client, new { cantidad = 0 });
        Assert.Empty((await app.JsonAsync<SalidaResponse>(HttpMethod.Get, $"/api/salidas/{cart.Id}", inventory.Client)).Detalles);
        await app.StatusAsync(HttpMethod.Post, path, HttpStatusCode.NoContent, inventory.Client,
            new { productoId = inventory.ProductId, cantidad = 0 });
        var restored = await app.JsonAsync<DetalleSalidaResponse>(HttpMethod.Post, path, inventory.Client,
            new { productoId = inventory.ProductId, cantidad = 5 }, HttpStatusCode.Created);
        await app.StatusAsync(HttpMethod.Post, $"/api/salidas/{cart.Id}/confirmar-cliente", HttpStatusCode.OK, inventory.Client);
        await app.StatusAsync(HttpMethod.Put, path + $"/{restored.Id}", HttpStatusCode.Conflict, inventory.Client, new { cantidad = 0 });
        await app.StatusAsync(HttpMethod.Post, $"/api/salidas/{cart.Id}/confirmar", HttpStatusCode.OK, inventory.Employee);
        Assert.Equal(0, await StockAsync(app, inventory.ProductId));
        await app.DatabaseAsync(async db => Assert.False(await db.DetallesSalida.AnyAsync(d => d.Cantidad <= 0)));
    }

    [Fact]
    public async Task Salida_confirma_una_vez_compite_por_stock_y_revierte_fallos_parciales()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var inventory = await InventoryAsync(app, 5);
        var cart = await CartAsync(app, inventory.Client, [(inventory.ProductId, 2)], request: true);
        var statuses = await RaceAsync(
            () => app.SendAsync(HttpMethod.Post, $"/api/salidas/{cart.Id}/confirmar", app.Admin),
            () => app.SendAsync(HttpMethod.Post, $"/api/salidas/{cart.Id}/confirmar", inventory.Employee));
        Assert.Single(statuses, s => s == HttpStatusCode.OK);
        Assert.Single(statuses, s => s == HttpStatusCode.Conflict);
        Assert.Equal(3, await StockAsync(app, inventory.ProductId));
        await app.StatusAsync(HttpMethod.Delete, $"/api/salidas/{cart.Id}", HttpStatusCode.Conflict, inventory.Client);
        var other = await app.RegisterAsync(Usuario.RolCliente);
        var a = await CartAsync(app, inventory.Client, [(inventory.ProductId, 2)], request: true);
        var b = await CartAsync(app, other, [(inventory.ProductId, 2)], request: true);
        statuses = await RaceAsync(
            () => app.SendAsync(HttpMethod.Post, $"/api/salidas/{a.Id}/confirmar", app.Admin),
            () => app.SendAsync(HttpMethod.Post, $"/api/salidas/{b.Id}/confirmar", inventory.Employee));
        Assert.Single(statuses, s => s == HttpStatusCode.OK);
        Assert.Single(statuses, s => s == HttpStatusCode.Conflict);
        Assert.Equal(1, await StockAsync(app, inventory.ProductId));
        await app.StatusAsync(HttpMethod.Delete, $"/api/ingresos/{inventory.SeedIncomeId}", HttpStatusCode.Conflict, inventory.Employee);

        var p1 = await ProductAsync(app, inventory.CategoryId);
        var p2 = await ProductAsync(app, inventory.CategoryId, "Producto para fallo controlado");
        await IncomeAsync(app, inventory, [(p1, 3), (p2, 3)], confirm: true);
        var failing = await CartAsync(app, inventory.Client, [(p1, 1), (p2, 1)], request: true);
        // The second stock update violates a constraint; the first and header must roll back.
        await app.DatabaseAsync(db => db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE Productos ADD CONSTRAINT CK_TestFailStock CHECK (Nombre <> N'Producto para fallo controlado' OR Stock >= 3);"));
        await app.StatusAsync(HttpMethod.Post, $"/api/salidas/{failing.Id}/confirmar", HttpStatusCode.InternalServerError, inventory.Employee);
        Assert.Equal(3, await StockAsync(app, p1));
        Assert.Equal(3, await StockAsync(app, p2));
        var unchanged = await app.JsonAsync<SalidaResponse>(HttpMethod.Get, $"/api/salidas/{failing.Id}", inventory.Client);
        Assert.False(unchanged.ConfirmarXEmpleado);
        Assert.Null(unchanged.UsuarioConfirmacionId);
        await app.DatabaseAsync(db => db.Database.ExecuteSqlRawAsync("ALTER TABLE Productos DROP CONSTRAINT CK_TestFailStock;"));
        await app.StatusAsync(HttpMethod.Post, $"/api/salidas/{failing.Id}/confirmar", HttpStatusCode.OK, inventory.Employee);
        Assert.Equal(2, await StockAsync(app, p1));
        Assert.Equal(2, await StockAsync(app, p2));
    }

    [Fact]
    public async Task Imagen_valida_se_reemplaza_sin_duplicados_y_archivos_invalidos_se_rechazan()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var inventory = await InventoryAsync(app);
        // Fixed one-pixel PNG fixture, independent of external files or image generators.
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGMQUDAAAACkAGE0Zn1yAAAAAElFTkSuQmCC");
        async Task<HttpResponseMessage> Upload(byte[] bytes, string mime = "image/png")
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/productos/{inventory.ProductId}/imagen");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", inventory.Employee.Token);
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue(mime);
            form.Add(file, "File", "test.png");
            request.Content = form;
            return await app.Http.SendAsync(request);
        }
        using var first = await Upload(png);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var original = (await first.Content.ReadFromJsonAsync<ImagenResponse>())!;
        using var second = await Upload(png);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var replacement = (await second.Content.ReadFromJsonAsync<ImagenResponse>())!;
        Assert.Equal(original.Id, replacement.Id);
        Assert.NotEqual(original.ImagenUrl, replacement.ImagenUrl);
        using var oldFile = await app.Http.GetAsync(original.ImagenUrl);
        Assert.Equal(HttpStatusCode.NotFound, oldFile.StatusCode);
        using var newFile = await app.Http.GetAsync(replacement.ImagenUrl);
        Assert.Equal(HttpStatusCode.OK, newFile.StatusCode);
        Assert.Equal("nosniff", Assert.Single(newFile.Headers.GetValues("X-Content-Type-Options")));
        using var corrupt = await Upload(Encoding.UTF8.GetBytes("not-a-valid-image"));
        Assert.Equal(HttpStatusCode.BadRequest, corrupt.StatusCode);
        using var wrongMime = await Upload(png, "image/jpeg");
        Assert.Equal(HttpStatusCode.BadRequest, wrongMime.StatusCode);
        Assert.Single(Directory.GetFiles(app.Uploads));
        await app.DatabaseAsync(async db => Assert.Equal(1, await db.Imagenes.CountAsync(i => i.ProductoId == inventory.ProductId)));
    }

    [Fact]
    public async Task Importes_redondean_por_detalle_y_conservan_precio_historico_en_auditoria()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var inventory = await InventoryAsync(app);
        async Task Price(decimal price) => await app.StatusAsync(HttpMethod.Put,
            $"/api/productos/{inventory.ProductId}", HttpStatusCode.NoContent, app.Admin,
            new { nombre = "Producto", precio = price, categoriaId = inventory.CategoryId });
        await Price(0.01m);
        var income = await IncomeAsync(app, inventory, []);
        foreach (var discount in new[] { 50m, 50m, 0m, 100m })
            await app.StatusAsync(HttpMethod.Post, $"/api/ingresos/{income.Id}/detalles", HttpStatusCode.Created,
                inventory.Employee, new { productoId = inventory.ProductId, cantidad = 1, descAplicado = discount });
        income = await app.JsonAsync<IngresoResponse>(HttpMethod.Post, $"/api/ingresos/{income.Id}/confirmar", inventory.Employee);
        Assert.Equal(new[] { 0.01m, 0.01m, 0.01m, 0m }, income.Detalles.Select(d => d.Subtotal));
        Assert.Equal(0.03m, income.Total); // Rounding the aggregate would produce 0.02.
        await Price(0.03m);
        var cart = await CartAsync(app, inventory.Client, [(inventory.ProductId, 1)]);
        await Price(0.99m);
        var detail = Assert.Single(cart.Detalles);
        await app.StatusAsync(HttpMethod.Put, $"/api/salidas/{cart.Id}/detalles/{detail.Id}", HttpStatusCode.OK,
            inventory.Client, new { cantidad = 2 });
        await app.StatusAsync(HttpMethod.Post, $"/api/salidas/{cart.Id}/confirmar-cliente", HttpStatusCode.OK, inventory.Client);
        cart = await app.JsonAsync<SalidaResponse>(HttpMethod.Post, $"/api/salidas/{cart.Id}/confirmar", inventory.Employee);
        Assert.Equal(0.03m, Assert.Single(cart.Detalles).PrecioUnitario);
        Assert.Equal(0.06m, cart.Total);
        var historicalIncome = await app.JsonAsync<IngresoResponse>(HttpMethod.Get, $"/api/ingresos/{income.Id}", inventory.Employee);
        Assert.Equal(0.03m, historicalIncome.Total);
        Assert.All(historicalIncome.Detalles, d => Assert.Equal(0.01m, d.PrecioUnitario));
        var auditIncome = await app.JsonAsync<AuditoriaIngresoResponse>(HttpMethod.Get, $"/api/auditoria/ingresos/{income.Id}", app.Admin);
        Assert.Equal(income.Total, auditIncome.Total);
        Assert.Equal(income.Detalles.Select(d => d.Subtotal), auditIncome.Detalles.Select(d => d.Subtotal));
        var auditSale = await app.JsonAsync<AuditoriaSalidaResponse>(HttpMethod.Get, $"/api/auditoria/salidas/{cart.Id}", app.Admin);
        Assert.Equal(cart.Total, auditSale.Total);
        Assert.Equal(0.03m, Assert.Single(auditSale.Detalles).PrecioUnitario);
        var path = $"/api/auditoria/productos/{inventory.ProductId}/movimientos";
        var sales = await app.JsonAsync<PaginatedResult<AuditoriaMovimientoResponse>>(HttpMethod.Get, path + "?tipo=Salida&pageSize=1", app.Admin);
        Assert.Equal(1, sales.TotalCount);
        Assert.Equal(0.06m, Assert.Single(sales.Items).Subtotal);
        var first = await app.JsonAsync<PaginatedResult<AuditoriaMovimientoResponse>>(HttpMethod.Get, path + "?tipo=Ingreso&pageSize=1", app.Admin);
        var second = await app.JsonAsync<PaginatedResult<AuditoriaMovimientoResponse>>(HttpMethod.Get, path + "?tipo=Ingreso&pageSize=1&page=2", app.Admin);
        Assert.Equal(4, first.TotalCount);
        Assert.Equal(4, second.TotalCount);
        Assert.NotEqual(Assert.Single(first.Items).DetalleId, Assert.Single(second.Items).DetalleId);
        foreach (var row in first.Items.Concat(second.Items))
            Assert.Equal(income.Detalles.Single(d => d.Id == row.DetalleId).Subtotal, row.Subtotal);
    }

    [Fact]
    public async Task Limites_monetarios_rechazan_excesos_sin_persistir_detalles_parciales()
    {
        const decimal max = 9999999999999999.99m;
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var inventory = await InventoryAsync(app);
        await app.StatusAsync(HttpMethod.Put, $"/api/productos/{inventory.ProductId}", HttpStatusCode.NoContent, app.Admin,
            new { nombre = "Producto", precio = max, categoriaId = inventory.CategoryId });
        var seed = await IncomeAsync(app, inventory, [(inventory.ProductId, 1)], confirm: true);
        Assert.Equal(max, seed.Total);
        var cheap = await ProductAsync(app, inventory.CategoryId, "Otro producto");
        await app.StatusAsync(HttpMethod.Put, $"/api/productos/{cheap}", HttpStatusCode.NoContent, app.Admin,
            new { nombre = "Otro producto", precio = 0.01m, categoriaId = inventory.CategoryId });
        await IncomeAsync(app, inventory, [(cheap, 1)], confirm: true);
        var income = await IncomeAsync(app, inventory, [(inventory.ProductId, 1)]);
        var cart = await CartAsync(app, inventory.Client, [(inventory.ProductId, 1)]);
        foreach (var (path, actor, message) in new[]
        {
            ($"/api/ingresos/{income.Id}/detalles", inventory.Employee, "El total del ingreso excede el rango decimal(18,2)."),
            ($"/api/salidas/{cart.Id}/detalles", inventory.Client, "El total de la salida excede el rango decimal(18,2).")
        })
        {
            using var rejected = await app.SendAsync(HttpMethod.Post, path, actor, new { productoId = cheap, cantidad = 1 });
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
            var error = await rejected.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(message, error.GetProperty("error").GetString());
        }
        foreach (var discount in new[] { -0.01m, 100.01m, 0.001m })
            await app.StatusAsync(HttpMethod.Post, $"/api/ingresos/{income.Id}/detalles", HttpStatusCode.BadRequest,
                inventory.Employee, new { productoId = cheap, cantidad = 1, descAplicado = discount });
        foreach (var price in new[] { -0.01m, 10000000000000000m, 0.001m })
            await app.StatusAsync(HttpMethod.Put, $"/api/productos/{cheap}", HttpStatusCode.BadRequest, app.Admin,
                new { nombre = "Otro producto", precio = price, categoriaId = inventory.CategoryId });
        await app.StatusAsync(HttpMethod.Post, $"/api/ingresos/{income.Id}/detalles", HttpStatusCode.BadRequest,
            inventory.Employee, new { productoId = inventory.ProductId, cantidad = 2 });
        var preservedIncome = await app.JsonAsync<IngresoResponse>(HttpMethod.Get, $"/api/ingresos/{income.Id}", inventory.Employee);
        var preservedCart = await app.JsonAsync<SalidaResponse>(HttpMethod.Get, $"/api/salidas/{cart.Id}", inventory.Client);
        Assert.Equal(max, preservedIncome.Total);
        Assert.Single(preservedIncome.Detalles);
        Assert.Equal(max, preservedCart.Total);
        Assert.Single(preservedCart.Detalles);
        var free = await IncomeAsync(app, inventory, [(inventory.ProductId, int.MaxValue)], discount: 100m);
        Assert.Equal(0m, free.Total);
        Assert.Equal(int.MaxValue, Assert.Single(free.Detalles).Cantidad);
        await app.StatusAsync(HttpMethod.Post, $"/api/salidas/{cart.Id}/detalles", HttpStatusCode.Conflict,
            inventory.Client, new { productoId = inventory.ProductId, cantidad = int.MaxValue });
        Assert.Equal(1, await StockAsync(app, inventory.ProductId));
        Assert.Equal(1, await StockAsync(app, cheap));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cliente_consulta_cancelados_propios_sin_poder_modificarlos(bool solicitado)
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var inventory = await InventoryAsync(app, stock: 2);
        var other = await app.RegisterAsync(Usuario.RolCliente);
        var cart = await CartAsync(app, inventory.Client, [(inventory.ProductId, 1)], request: solicitado);
        var detail = Assert.Single(cart.Detalles);
        Assert.False(cart.Eliminado);
        await app.StatusAsync(HttpMethod.Delete, $"/api/salidas/{cart.Id}", HttpStatusCode.NoContent, inventory.Client);
        var canceled = await app.JsonAsync<SalidaResponse>(HttpMethod.Get, $"/api/salidas/{cart.Id}", inventory.Client);
        Assert.True(canceled.Eliminado);
        Assert.Equal(solicitado, canceled.ConfirmarXCliente);
        Assert.False(canceled.ConfirmarXEmpleado);
        Assert.Equal(cart.Total, canceled.Total);
        Assert.Equal(detail, Assert.Single(canceled.Detalles));
        var page = await app.JsonAsync<PaginatedResult<SalidaResponse>>(HttpMethod.Get,
            $"/api/salidas?productoId={inventory.ProductId}&pageSize=1", inventory.Client);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(canceled, Assert.Single(page.Items) with { Detalles = canceled.Detalles });
        Assert.Equal(detail, await app.JsonAsync<DetalleSalidaResponse>(HttpMethod.Get,
            $"/api/salidas/{cart.Id}/detalles/{detail.Id}", inventory.Client));
        foreach (var actor in new[] { other, app.Admin, inventory.Employee })
        {
            var hidden = await app.JsonAsync<PaginatedResult<SalidaResponse>>(HttpMethod.Get,
                $"/api/salidas?clienteId={inventory.Client.Id}", actor);
            Assert.Empty(hidden.Items);
            Assert.Equal(0, hidden.TotalCount);
            await app.StatusAsync(HttpMethod.Get, $"/api/salidas/{cart.Id}", HttpStatusCode.NotFound, actor);
            await app.StatusAsync(HttpMethod.Get, $"/api/salidas/{cart.Id}/detalles/{detail.Id}", HttpStatusCode.NotFound, actor);
        }
        await app.StatusAsync(HttpMethod.Post, $"/api/salidas/{cart.Id}/detalles", HttpStatusCode.NotFound,
            inventory.Client, new { productoId = inventory.ProductId, cantidad = 1 });
        await app.StatusAsync(HttpMethod.Put, $"/api/salidas/{cart.Id}/detalles/{detail.Id}", HttpStatusCode.NotFound,
            inventory.Client, new { cantidad = 0 });
        await app.StatusAsync(HttpMethod.Delete, $"/api/salidas/{cart.Id}/detalles/{detail.Id}", HttpStatusCode.NotFound, inventory.Client);
        await app.StatusAsync(HttpMethod.Delete, $"/api/salidas/{cart.Id}", HttpStatusCode.NotFound, inventory.Client);
        await app.StatusAsync(HttpMethod.Post, $"/api/salidas/{cart.Id}/confirmar-cliente", HttpStatusCode.NotFound, inventory.Client);
        await app.StatusAsync(HttpMethod.Post, $"/api/salidas/{cart.Id}/confirmar", HttpStatusCode.NotFound, inventory.Employee);
        Assert.Equal(2, await StockAsync(app, inventory.ProductId));
    }

    private sealed record Inventory(TestAccount Employee, TestAccount Client, int CategoryId,
        int SupplierId, int ProductId, int? SeedIncomeId);

    private static async Task<Inventory> InventoryAsync(TestApplication app, int stock = 0)
    {
        var employee = await app.RegisterAsync(Usuario.RolEmpleado);
        var client = await app.RegisterAsync(Usuario.RolCliente);
        var category = await app.JsonAsync<CategoriaResponse>(HttpMethod.Post, "/api/categorias", app.Admin,
            new { nombre = "Categoria", descripcion = "Prueba" }, HttpStatusCode.Created);
        var supplier = await app.JsonAsync<ProveedorResponse>(HttpMethod.Post, "/api/proveedores", employee,
            new { nombre = "Proveedor", cuit = "30123456781" }, HttpStatusCode.Created);
        var product = await ProductAsync(app, category.Id);
        var inventory = new Inventory(employee, client, category.Id, supplier.Id, product, null);
        if (stock > 0)
        {
            var income = await IncomeAsync(app, inventory, [(product, stock)], confirm: true);
            inventory = inventory with { SeedIncomeId = income.Id };
        }
        return inventory;
    }

    private static async Task<int> ProductAsync(TestApplication app, int category, string name = "Producto") =>
        (await app.JsonAsync<ProductoResponse>(HttpMethod.Post, "/api/productos", app.Admin,
            new { nombre = name, precio = 12.35m, categoriaId = category }, HttpStatusCode.Created)).Id;

    private static async Task<int> StockAsync(TestApplication app, int product) =>
        (await app.JsonAsync<ProductoResponse>(HttpMethod.Get, $"/api/productos/{product}", app.Admin)).Stock;

    private static async Task<IngresoResponse> IncomeAsync(TestApplication app, Inventory inventory,
        (int Product, int Quantity)[] rows, bool confirm = false, decimal discount = 0)
    {
        var header = await app.JsonAsync<IngresoResponse>(HttpMethod.Post, "/api/ingresos", inventory.Employee,
            new { proveedorId = inventory.SupplierId }, HttpStatusCode.Created);
        foreach (var (product, quantity) in rows)
            await app.StatusAsync(HttpMethod.Post, $"/api/ingresos/{header.Id}/detalles", HttpStatusCode.Created,
                inventory.Employee, new { productoId = product, cantidad = quantity, descAplicado = discount });
        return confirm
            ? await app.JsonAsync<IngresoResponse>(HttpMethod.Post, $"/api/ingresos/{header.Id}/confirmar", inventory.Employee)
            : await app.JsonAsync<IngresoResponse>(HttpMethod.Get, $"/api/ingresos/{header.Id}", inventory.Employee);
    }

    private static async Task<SalidaResponse> CartAsync(TestApplication app, TestAccount client,
        (int Product, int Quantity)[] rows, bool request = false)
    {
        var cart = await app.JsonAsync<SalidaResponse>(HttpMethod.Post, "/api/salidas", client,
            new { observaciones = "Prueba" }, HttpStatusCode.Created);
        foreach (var (product, quantity) in rows)
            await app.StatusAsync(HttpMethod.Post, $"/api/salidas/{cart.Id}/detalles", HttpStatusCode.Created,
                client, new { productoId = product, cantidad = quantity });
        return request
            ? await app.JsonAsync<SalidaResponse>(HttpMethod.Post, $"/api/salidas/{cart.Id}/confirmar-cliente", client)
            : await app.JsonAsync<SalidaResponse>(HttpMethod.Get, $"/api/salidas/{cart.Id}", client);
    }

    private static async Task<HttpStatusCode[]> RaceAsync(Func<Task<HttpResponseMessage>> first,
        Func<Task<HttpResponseMessage>> second)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpStatusCode> Run(Func<Task<HttpResponseMessage>> action)
        {
            await gate.Task;
            using var response = await action();
            return response.StatusCode;
        }
        var tasks = new[] { Run(first), Run(second) };
        gate.SetResult();
        return await Task.WhenAll(tasks);
    }
}
