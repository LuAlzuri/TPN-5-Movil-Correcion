using TPN_5_MOVIL.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Exceptions;
using TPN_5_MOVIL.Services;

namespace TPN_5_MOVIL.Controllers;

[ApiController]
[Route("api/productos")]
[Authorize(Roles = GruposRoles.Todos)]
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ImagenProductoService _imagenes;

    public ProductosController(AppDbContext db, ImagenProductoService imagenes)
    {
        _db = db;
        _imagenes = imagenes;
    }

    // Preserve category/image metadata even if the category was soft deleted, while
    // explicitly retaining the operational filter on the product itself.
    private IQueryable<Producto> ProductosActivos => _db.Productos.IgnoreQueryFilters().Where(p => !p.Eliminado);

    private static IQueryable<ProductoResponse> Respuestas(IQueryable<Producto> query) =>
        query.Select(p => new ProductoResponse(p.Id, p.Nombre, p.Descripcion, p.Precio, p.Stock,
            p.Imagen == null ? null : p.Imagen.RutaRelativa, p.CategoriaId, p.Categoria.Nombre));

    private static void ValidarPagina(int page, int pageSize)
    {
        Paginacion.Validar(page, pageSize);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ProductoResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? busqueda = null,
        [FromQuery] int? categoriaId = null,
        [FromQuery] int? proveedorId = null)
    {
        ValidarPagina(page, pageSize);
        if (categoriaId <= 0 || proveedorId <= 0)
            throw new ApiException("Los filtros de categoría y proveedor deben ser positivos.");
        busqueda = busqueda?.Trim();
        var query = ProductosActivos
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(p => p.Nombre.Contains(busqueda) || (p.Descripcion != null && p.Descripcion.Contains(busqueda)));

        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == categoriaId);

        if (proveedorId.HasValue)
            query = query.Where(p => p.DetallesIngreso.Any(d => !d.Ingreso.Eliminado && d.Ingreso.ProveedorId == proveedorId.Value));

        query = query.OrderBy(p => p.Nombre).ThenBy(p => p.Id);

        var result = await PaginatedResult<ProductoResponse>.CreateAsync(
            Respuestas(query),
            page,
            pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductoResponse>> GetById(int id)
    {
        var producto = await Respuestas(ProductosActivos.AsNoTracking().Where(p => p.Id == id))
            .FirstOrDefaultAsync();

        return producto is null ? NotFound() : Ok(producto);
    }

    [HttpGet("{id:int}/movimientos")]
    [Authorize(Roles = GruposRoles.Personal)]
    public async Task<ActionResult<PaginatedResult<MovimientoItem>>> GetMovimientos(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        ValidarPagina(page, pageSize);
        if (!await _db.Productos.AnyAsync(p => p.Id == id))
            return NotFound();

        var ingresos = _db.DetallesIngreso.IgnoreQueryFilters()
            .Where(d => d.ProductoId == id && !d.Ingreso.Eliminado && d.Ingreso.Confirmado)
            .Select(d => new
            {
                Id = d.IngresoId, DetalleId = d.Id, Tipo = "Ingreso", Fecha = d.Ingreso.Fecha,
                Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario,
                Contraparte = d.Ingreso.Proveedor.Nombre, Observaciones = d.Ingreso.Observaciones
            });
        var salidas = _db.DetallesSalida.IgnoreQueryFilters()
            .Where(d => d.ProductoId == id && !d.Salida.Eliminado && d.Salida.ConfirmarXEmpleado)
            .Select(d => new
            {
                Id = d.SalidaId, DetalleId = d.Id, Tipo = "Salida", Fecha = d.Salida.Fecha,
                Cantidad = -d.Cantidad, PrecioUnitario = d.PrecioUnitario,
                Contraparte = d.Salida.Cliente.Usuario.Nombre + " " + d.Salida.Cliente.Usuario.Apellido,
                Observaciones = d.Salida.Observaciones
            });

        var combinados = ingresos
            .Concat(salidas)
            .OrderByDescending(m => m.Fecha)
            .ThenBy(m => m.Tipo).ThenBy(m => m.Id).ThenBy(m => m.DetalleId)
            .Select(m => new MovimientoItem(
                m.Id,
                m.Tipo,
                m.Fecha,
                m.Cantidad,
                m.PrecioUnitario,
                m.Contraparte,
                m.Observaciones));

        var result = await PaginatedResult<MovimientoItem>.CreateAsync(combinados, page, pageSize);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = GruposRoles.Personal)]
    public async Task<ActionResult<ProductoResponse>> Create([FromBody] ProductoCreateRequest request)
    {
        if (!await _db.Categorias.AnyAsync(c => c.Id == request.CategoriaId!.Value))
            throw new ApiException("La categoría indicada no existe.", StatusCodes.Status404NotFound);

        var producto = new Producto
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim(),
            Precio = request.Precio!.Value,
            Stock = 0,
            CategoriaId = request.CategoriaId!.Value,
            FechaCreacion = DateTime.UtcNow
        };

        _db.Productos.Add(producto);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = producto.Id }, await ConstruirResponseAsync(producto.Id));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = GruposRoles.Personal)]
    public async Task<IActionResult> Update(int id, [FromBody] ProductoUpdateRequest request)
    {
        var producto = await _db.Productos.FindAsync(id);
        if (producto is null)
            return NotFound();

        if (producto.CategoriaId != request.CategoriaId && !await _db.Categorias.AnyAsync(c => c.Id == request.CategoriaId!.Value))
            throw new ApiException("La categoría indicada no existe.", StatusCodes.Status404NotFound);

        producto.Nombre = request.Nombre.Trim();
        producto.Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim();
        producto.Precio = request.Precio!.Value;
        producto.CategoriaId = request.CategoriaId!.Value;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = GruposRoles.Personal)]
    public async Task<IActionResult> Delete(int id)
    {
        var producto = await _db.Productos.FindAsync(id);
        if (producto is null)
            return NotFound();

        producto.Eliminado = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/imagen")]
    [Authorize(Roles = GruposRoles.Personal)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImagenProductoService.MaxPeticion)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImagenProductoService.MaxPeticion)]
    public async Task<ActionResult<ImagenResponse>> UploadImagen(int id, [FromForm] ImagenUploadRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _imagenes.GuardarAsync(id, request.File!, cancellationToken));
    }

    private async Task<ProductoResponse?> ConstruirResponseAsync(int id)
    {
        return await Respuestas(ProductosActivos.AsNoTracking().Where(p => p.Id == id))
            .FirstOrDefaultAsync();
    }

}
