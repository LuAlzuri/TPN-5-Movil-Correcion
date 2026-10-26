using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Exceptions;

namespace TPN_5_MOVIL.Controllers;

[ApiController]
[Route("api/productos")]
[Authorize]
public class ProductosController : ControllerBase
{
    private const int MaxTamanoImagen = 5 * 1024 * 1024;

    private readonly AppDbContext _db;

    public ProductosController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ProductoResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? busqueda = null,
        [FromQuery] int? categoriaId = null,
        [FromQuery] int? proveedorId = null)
    {
        var query = _db.Productos
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(p => p.Nombre.Contains(busqueda) || p.Descripcion!.Contains(busqueda));

        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == categoriaId);

        if (proveedorId.HasValue)
            query = query.Where(p => p.DetallesIngreso.Any(d => d.Ingreso.ProveedorId == proveedorId.Value));

        query = query.OrderBy(p => p.Nombre);

        var result = await PaginatedResult<ProductoResponse>.CreateAsync(
            query.Select(p => new ProductoResponse(
                p.Id,
                p.Nombre,
                p.Descripcion,
                p.Precio,
                p.Stock,
                p.Imagen == null ? null : p.Imagen.RutaRelativa,
                p.CategoriaId,
                p.Categoria.Nombre)),
            page,
            pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductoResponse>> GetById(int id)
    {
        var producto = await _db.Productos
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductoResponse(
                p.Id,
                p.Nombre,
                p.Descripcion,
                p.Precio,
                p.Stock,
                p.Imagen == null ? null : p.Imagen.RutaRelativa,
                p.CategoriaId,
                p.Categoria.Nombre))
            .FirstOrDefaultAsync();

        return producto is null ? NotFound() : Ok(producto);
    }

    [HttpGet("{id:int}/movimientos")]
    [Authorize(Roles = Usuario.RolAdministrador + "," + Usuario.RolEmpleado)]
    public async Task<ActionResult<PaginatedResult<MovimientoItem>>> GetMovimientos(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!await _db.Productos.AnyAsync(p => p.Id == id))
            return NotFound();

        var ingresos = _db.DetallesIngreso
            .Where(d => d.ProductoId == id && d.Ingreso.Confirmado)
            .Select(d => new
            {
                Id = d.IngresoId, Tipo = "Ingreso", Fecha = d.Ingreso.Fecha,
                Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario,
                Contraparte = d.Ingreso.Proveedor.Nombre, Observaciones = d.Ingreso.Observaciones
            });
        var salidas = _db.DetallesSalida
            .Where(d => d.ProductoId == id && d.Salida.ConfirmarXEmpleado)
            .Select(d => new
            {
                Id = d.SalidaId, Tipo = "Salida", Fecha = d.Salida.Fecha,
                Cantidad = -d.Cantidad, PrecioUnitario = d.PrecioUnitario,
                Contraparte = d.Salida.Cliente.Usuario.Nombre + " " + d.Salida.Cliente.Usuario.Apellido,
                Observaciones = d.Salida.Observaciones
            });

        var combinados = ingresos
            .Concat(salidas)
            .OrderByDescending(m => m.Fecha)
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
    [Authorize(Roles = Usuario.RolAdministrador + "," + Usuario.RolEmpleado)]
    public async Task<ActionResult<ProductoResponse>> Create([FromBody] ProductoCreateRequest request)
    {
        if (request.Precio < 0)
            throw new ApiException("El precio no puede ser negativo.");

        if (!await _db.Categorias.AnyAsync(c => c.Id == request.CategoriaId))
            throw new ApiException("La categoría indicada no existe.", StatusCodes.Status404NotFound);

        var producto = new Producto
        {
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            Precio = request.Precio,
            Stock = 0,
            CategoriaId = request.CategoriaId,
            FechaCreacion = DateTime.UtcNow
        };

        _db.Productos.Add(producto);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = producto.Id }, await ConstruirResponseAsync(producto.Id));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Usuario.RolAdministrador + "," + Usuario.RolEmpleado)]
    public async Task<IActionResult> Update(int id, [FromBody] ProductoUpdateRequest request)
    {
        var producto = await _db.Productos.FindAsync(id);
        if (producto is null)
            return NotFound();

        if (request.Precio < 0)
            throw new ApiException("El precio no puede ser negativo.");

        if (!await _db.Categorias.AnyAsync(c => c.Id == request.CategoriaId))
            throw new ApiException("La categoría indicada no existe.", StatusCodes.Status404NotFound);

        producto.Nombre = request.Nombre;
        producto.Descripcion = request.Descripcion;
        producto.Precio = request.Precio;
        producto.CategoriaId = request.CategoriaId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Usuario.RolAdministrador + "," + Usuario.RolEmpleado)]
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
    [Authorize(Roles = Usuario.RolAdministrador + "," + Usuario.RolEmpleado)]
    [RequestSizeLimit(MaxTamanoImagen + 1024)]
    public IActionResult UploadImagen(int id, IFormFile file)
    {
        throw new ApiException("La carga de imágenes con metadatos está pendiente de implementación.", StatusCodes.Status501NotImplemented);
    }

    private async Task<ProductoResponse?> ConstruirResponseAsync(int id)
    {
        return await _db.Productos
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductoResponse(
                p.Id,
                p.Nombre,
                p.Descripcion,
                p.Precio,
                p.Stock,
                p.Imagen == null ? null : p.Imagen.RutaRelativa,
                p.CategoriaId,
                p.Categoria.Nombre))
            .FirstOrDefaultAsync();
    }

}
