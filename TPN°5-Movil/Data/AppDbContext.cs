using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Entities;

namespace TPN_5_MOVIL.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private const string CuitNormalizadoSql = "REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([CUIT], '-', ''), ' ', ''), CHAR(9), ''), CHAR(10), ''), CHAR(13), '')";
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<Imagen> Imagenes => Set<Imagen>();

        public DbSet<Ingreso> Ingresos => Set<Ingreso>();
        public DbSet<DetalleIngreso> DetallesIngreso => Set<DetalleIngreso>();
        public DbSet<Salida> Salidas => Set<Salida>();
        public DbSet<DetalleSalida> DetallesSalida => Set<DetalleSalida>();
        public DbSet<AuditoriaAccion> AuditoriaAcciones => Set<AuditoriaAccion>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios", t => t.HasCheckConstraint("CK_Usuario_Rol", "[Rol] IN ('Administrador','Empleado','Cliente')"));
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.Apellido).HasMaxLength(150).IsRequired();
            e.Property(x => x.CUIT).HasMaxLength(20).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.Rol).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property<string>("CUITNormalizado").HasMaxLength(20)
                .HasComputedColumnSql(CuitNormalizadoSql, stored: true);
            e.HasIndex("CUITNormalizado").IsUnique().HasFilter("[Rol] = 'Cliente'")
                .HasDatabaseName("IX_Usuarios_CUITNormalizado_Cliente");
            e.HasQueryFilter(x => !x.Eliminado);
        });
        modelBuilder.Entity<Empleado>(e =>
        {
            e.ToTable("Empleados");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasOne(x => x.Usuario).WithOne(x => x.Empleado).HasForeignKey<Empleado>(x => x.Id);
            e.HasQueryFilter(x => !x.Usuario.Eliminado);
        });
        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("Clientes");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Telefono).HasMaxLength(50);
            e.Property(x => x.Direccion).HasMaxLength(300);
            e.HasOne(x => x.Usuario).WithOne(x => x.Cliente).HasForeignKey<Cliente>(x => x.Id);
            e.HasQueryFilter(x => !x.Usuario.Eliminado);
        });
        modelBuilder.Entity<Categoria>(e =>
        {
            e.ToTable("Categorias");
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Nombre).IsUnique();
            e.HasQueryFilter(x => !x.Eliminado);
        });
        modelBuilder.Entity<Proveedor>(e =>
        {
            e.ToTable("Proveedores");
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.CUIT).HasMaxLength(20).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150);
            e.Property(x => x.Telefono).HasMaxLength(50);
            e.Property(x => x.Direccion).HasMaxLength(300);
            e.HasIndex(x => x.CUIT).IsUnique();
            e.Property<string>("CUITNormalizado").HasMaxLength(20)
                .HasComputedColumnSql(CuitNormalizadoSql, stored: true);
            e.HasIndex("CUITNormalizado").IsUnique().HasFilter(null);
            e.HasQueryFilter(x => !x.Eliminado);
        });
        modelBuilder.Entity<Producto>(e =>
        {
            e.ToTable("Productos", t =>
            {
                t.HasCheckConstraint("CK_Producto_Stock", "[Stock] >= 0");
                t.HasCheckConstraint("CK_Producto_Precio", "[Precio] >= 0");
            });
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.Precio).HasPrecision(18, 2);
            e.HasIndex(x => x.Nombre);
            e.HasOne(x => x.Categoria).WithMany(x => x.Productos).HasForeignKey(x => x.CategoriaId);
            e.HasQueryFilter(x => !x.Eliminado);
        });
        modelBuilder.Entity<Imagen>(e =>
        {
            e.ToTable("Imagenes", t => t.HasCheckConstraint("CK_Imagen_Tamano", "[TamanoBytes] > 0"));
            e.Property(x => x.NombreArchivo).HasMaxLength(255).IsRequired();
            e.Property(x => x.RutaRelativa).HasMaxLength(500).IsRequired();
            e.Property(x => x.TipoContenido).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Producto).WithOne(x => x.Imagen).HasForeignKey<Imagen>(x => x.ProductoId);
            e.HasQueryFilter(x => !x.Producto.Eliminado);
        });
        modelBuilder.Entity<Ingreso>(e =>
        {
            e.ToTable("Ingresos", t => t.HasCheckConstraint("CK_Ingreso_Confirmacion",
                "([Confirmado] = 0 AND [FechaConfirmacion] IS NULL AND [EmpleadoConfirmacionId] IS NULL) OR ([Confirmado] = 1 AND [FechaConfirmacion] IS NOT NULL AND [EmpleadoConfirmacionId] IS NOT NULL)"));
            e.HasOne(x => x.Proveedor).WithMany(x => x.Ingresos).HasForeignKey(x => x.ProveedorId);
            e.HasOne(x => x.Empleado).WithMany(x => x.Ingresos).HasForeignKey(x => x.EmpleadoId);
            e.HasOne(x => x.EmpleadoConfirmacion).WithMany(x => x.IngresosConfirmados).HasForeignKey(x => x.EmpleadoConfirmacionId);
            e.HasIndex(x => x.Fecha);
            e.HasQueryFilter(x => !x.Eliminado);
        });
        modelBuilder.Entity<Salida>(e =>
        {
            e.ToTable("Salidas", t =>
            {
                t.HasCheckConstraint("CK_Salida_Cliente", "([ConfirmarXCliente] = 0 AND [FechaConfirmacionCliente] IS NULL) OR ([ConfirmarXCliente] = 1 AND [FechaConfirmacionCliente] IS NOT NULL)");
                t.HasCheckConstraint("CK_Salida_Empleado", "([ConfirmarXEmpleado] = 0 AND [UsuarioConfirmacionId] IS NULL AND [FechaConfirmacionEmpleado] IS NULL) OR ([ConfirmarXEmpleado] = 1 AND [ConfirmarXCliente] = 1 AND [UsuarioConfirmacionId] IS NOT NULL AND [FechaConfirmacionEmpleado] IS NOT NULL)");
            });
            e.HasOne(x => x.Cliente).WithMany(x => x.Salidas).HasForeignKey(x => x.ClienteId);
            e.HasOne(x => x.UsuarioConfirmacion).WithMany().HasForeignKey(x => x.UsuarioConfirmacionId);
            e.HasIndex(x => x.Fecha);
            e.HasQueryFilter(x => !x.Eliminado);
        });
        modelBuilder.Entity<DetalleIngreso>(e =>
        {
            e.ToTable("DetallesIngreso", t =>
            {
                t.HasCheckConstraint("CK_DetalleIngreso_Cantidad", "[Cantidad] >= 1");
                t.HasCheckConstraint("CK_DetalleIngreso_Precio", "[PrecioUnitario] >= 0");
                t.HasCheckConstraint("CK_DetalleIngreso_Descuento", "[DescAplicado] BETWEEN 0 AND 100");
            });
            e.Property(x => x.PrecioUnitario).HasPrecision(18, 2);
            e.Property(x => x.DescAplicado).HasPrecision(5, 2);
            e.HasOne(x => x.Ingreso).WithMany(x => x.Detalles).HasForeignKey(x => x.IngresoId);
            e.HasOne(x => x.Producto).WithMany(x => x.DetallesIngreso).HasForeignKey(x => x.ProductoId);
            e.HasQueryFilter(x => !x.Ingreso.Eliminado);
        });
        modelBuilder.Entity<DetalleSalida>(e =>
        {
            e.ToTable("DetallesSalida", t =>
            {
                t.HasCheckConstraint("CK_DetalleSalida_Cantidad", "[Cantidad] >= 1");
                t.HasCheckConstraint("CK_DetalleSalida_Precio", "[PrecioUnitario] >= 0");
                t.HasCheckConstraint("CK_DetalleSalida_Descuento", "[DescAplicado] BETWEEN 0 AND 100");
            });
            e.Property(x => x.PrecioUnitario).HasPrecision(18, 2);
            e.Property(x => x.DescAplicado).HasPrecision(5, 2);
            e.HasIndex(x => new { x.SalidaId, x.ProductoId }).IsUnique();
            e.HasOne(x => x.Salida).WithMany(x => x.Detalles).HasForeignKey(x => x.SalidaId);
            e.HasOne(x => x.Producto).WithMany(x => x.DetallesSalida).HasForeignKey(x => x.ProductoId);
            e.HasQueryFilter(x => !x.Salida.Eliminado);
        });
        // Preserve historical references; deletion is a business operation, never a cascade.
        modelBuilder.Entity<AuditoriaAccion>(entity =>
        {
            entity.ToTable("AuditoriaAcciones");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.UsuarioId).IsRequired();
            entity.Property(a => a.Accion).IsRequired().HasMaxLength(50);
            entity.Property(a => a.Entidad).IsRequired().HasMaxLength(50);
            entity.Property(a => a.EntidadId).HasMaxLength(50);
            entity.Property(a => a.UsuarioEmail).HasMaxLength(150);
            entity.Property(a => a.Ip).HasMaxLength(45);
            entity.Property(a => a.Observacion).HasMaxLength(250);
            entity.Property(a => a.FechaUtc).IsRequired();
            entity.HasIndex(a => a.FechaUtc);
            entity.HasIndex(a => new { a.Entidad, a.EntidadId });
            entity.HasIndex(a => new { a.UsuarioId, a.FechaUtc });
        });

        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
