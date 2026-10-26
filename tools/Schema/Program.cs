using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Entities;

using var db = new SchemaFactory().CreateDbContext([]);
var model = db.Model;
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    Console.WriteLine("PASS: " + message);
}
Check(model.GetEntityTypes().Count() == 11, "11 entidades mapeadas");
Check(model.GetEntityTypes().SelectMany(e => e.GetProperties()).All(p => p.Name != "RowVersion" && !p.IsConcurrencyToken), "Sin RowVersion ni tokens de concurrencia");
foreach (var type in new[] { typeof(Empleado), typeof(Cliente) })
{
    var entity = model.FindEntityType(type)!;
    Check(entity.GetForeignKeys().Any(f => f.PrincipalEntityType.ClrType == typeof(Usuario) && f.IsUnique && f.Properties.SequenceEqual(entity.FindPrimaryKey()!.Properties)), type.Name + ": PK compartida con Usuario");
}
Check(model.FindEntityType(typeof(Imagen))!.GetIndexes().Any(i => i.IsUnique && i.Properties.Single().Name == "ProductoId"), "Una imagen por producto");
Check(model.FindEntityType(typeof(DetalleSalida))!.GetIndexes().Any(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "SalidaId", "ProductoId" })), "Producto unico por carrito");
Check(model.FindEntityType(typeof(Salida))!.FindProperty("EmpleadoId")!.IsNullable, "Empleado de salida opcional antes de confirmar");
Check(model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()).All(f => f.DeleteBehavior == DeleteBehavior.Restrict), "Sin cascadas de eliminacion");
Check(db.Productos.ToQueryString().Contains("Eliminado"), "Filtro de productos traducido a SQL");
Check(!db.Productos.IgnoreQueryFilters().ToQueryString().Contains("WHERE"), "Auditoria puede omitir filtros");
Check(db.DetallesIngreso.ToQueryString().Contains("Eliminado"), "Detalles filtran encabezados eliminados");
Check(model.FindEntityType(typeof(Usuario))!.GetIndexes().Any(i => i.IsUnique
    && i.Properties.Single().Name == "CUITNormalizado" && i.GetFilter() == "[Rol] = 'Cliente'"), "CUIT unico solo entre Clientes");
Check(model.FindEntityType(typeof(Proveedor))!.GetIndexes().Any(i => i.IsUnique
    && i.Properties.Single().Name == "CUITNormalizado"), "CUIT unico entre Proveedores sin guiones ni espacios");
Check(db.Database.GetMigrations().Count() == 13, "13 migraciones descubiertas");
