using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Entities;

namespace TPN_5_MOVIL.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<AppDbContext>();

        if (await context.Categorias.AnyAsync())
            return;

        var categorias = new List<Categoria>
        {
            new() { Nombre = "Electrónica", Descripcion = "Dispositivos electrónicos y accesorios" },
            new() { Nombre = "Hogar", Descripcion = "Artículos para el hogar" },
            new() { Nombre = "Oficina", Descripcion = "Útiles y equipamiento de oficina" },
            new() { Nombre = "Deportes", Descripcion = "Indumentaria y accesorios deportivos" },
            new() { Nombre = "Alimentos", Descripcion = "Productos de almacén y bebidas" }
        };
        context.Categorias.AddRange(categorias);

        var proveedor1 = new Proveedor { Nombre = "Distribuidora Central SRL", CUIT = "30-71234567-8", Email = "ventas@central.com", Telefono = "+54 11 4000-0001", Direccion = "Av. Corrientes 1200, CABA" };
        var proveedor2 = new Proveedor { Nombre = "Mundo Import SA", CUIT = "30-66554433-2", Email = "info@mundoimport.com", Telefono = "+54 11 4000-0002", Direccion = "Buenos Aires 850, Rosario" };
        var proveedor3 = new Proveedor { Nombre = "Proveeduría Andina", CUIT = "27-33445566-7", Email = "contacto@andina.com", Telefono = "+54 261 400-0003", Direccion = "Mitre 210, Mendoza" };
        context.Proveedores.AddRange(proveedor1, proveedor2, proveedor3);

        var productos = new List<Producto>
        {
            new() { Nombre = "Notebook Gamer 15", Descripcion = "16GB RAM, SSD 512GB", Precio = 1850000m, Stock = 0, Categoria = categorias[0] },
            new() { Nombre = "Smart TV 50\" 4K", Descripcion = "HDR10, sistema operativo Android", Precio = 750000m, Stock = 0, Categoria = categorias[0] },
            new() { Nombre = "Auriculares Bluetooth", Descripcion = "Cancelación de ruido activa", Precio = 120000m, Stock = 0, Categoria = categorias[0] },
            new() { Nombre = "Cafetera Expresso", Descripcion = "15 bar, tanque 1.5L", Precio = 230000m, Stock = 0, Categoria = categorias[1] },
            new() { Nombre = "Juego de Ollas (10 piezas)", Descripcion = "Acero inoxidable", Precio = 185000m, Stock = 0, Categoria = categorias[1] },
            new() { Nombre = "Silla Ergonómica", Descripcion = "Soporte lumbar ajustable", Precio = 340000m, Stock = 0, Categoria = categorias[2] },
            new() { Nombre = "Escritorio Ejecutivo", Descripcion = "Madera, 1.40m", Precio = 290000m, Stock = 0, Categoria = categorias[2] },
            new() { Nombre = "Pelota de Fútbol", Descripcion = "Tamaño 5, cuero sintético", Precio = 45000m, Stock = 0, Categoria = categorias[3] },
            new() { Nombre = "Mancuernas (par 10kg)", Descripcion = "Neopreno antideslizante", Precio = 88000m, Stock = 0, Categoria = categorias[3] },
            new() { Nombre = "Yerba Mate Premium 1kg", Descripcion = "Con palo, estacionada", Precio = 8500m, Stock = 0, Categoria = categorias[4] }
        };
        context.Productos.AddRange(productos);

        await context.SaveChangesAsync();
    }
}