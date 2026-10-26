using System.Data;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Exceptions;

namespace TPN_5_MOVIL.Services;

public sealed class ImagenProductoService(AppDbContext db, IAlmacenImagenes almacen,
    ILogger<ImagenProductoService> logger)
{
    public const int MaxTamano = 5 * 1024 * 1024;
    public const int MaxPeticion = MaxTamano + 64 * 1024;
    public const long MaxPixeles = 20_000_000;

    public async Task<ImagenResponse> GuardarAsync(int productoId, IFormFile file, CancellationToken cancellationToken)
    {
        if (!await db.Productos.AnyAsync(p => p.Id == productoId, cancellationToken))
            throw new ApiException("El producto no existe.", StatusCodes.Status404NotFound);

        var (contenido, extension, mime) = await ValidarAsync(file, cancellationToken);
        var nombre = Guid.NewGuid().ToString("N") + extension;
        await almacen.GuardarAsync(nombre, contenido, cancellationToken);
        var commitIntentado = false;
        string? anterior = null;
        Imagen imagen;
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            // This conditional write locks the active product until commit, serializing image
            // replacement with other uploads and soft deletion, without a RowVersion column.
            var affected = await db.Productos.Where(p => p.Id == productoId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Nombre, p => p.Nombre), cancellationToken);
            if (affected != 1)
                throw new ApiException("El producto no existe.", StatusCodes.Status404NotFound);

            imagen = await db.Imagenes.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.ProductoId == productoId, cancellationToken)
                ?? new Imagen { ProductoId = productoId };
            anterior = imagen.Id == 0 ? null : imagen.NombreArchivo;
            if (imagen.Id == 0) db.Imagenes.Add(imagen);
            imagen.NombreArchivo = nombre;
            imagen.RutaRelativa = "/uploads/" + nombre;
            imagen.TipoContenido = mime;
            imagen.TamanoBytes = contenido.LongLength;
            await db.SaveChangesAsync(cancellationToken);
            commitIntentado = true;
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (!commitIntentado) almacen.IntentarEliminar(nombre);
            else logger.LogWarning("Confirmación de imagen con resultado incierto: se conservan ambos archivos para revisión.");
            throw;
        }

        if (anterior is not null) almacen.IntentarEliminar(anterior);
        return new ImagenResponse(imagen.Id, productoId, imagen.NombreArchivo, imagen.RutaRelativa,
            imagen.TipoContenido, imagen.TamanoBytes);
    }

    private static async Task<(byte[] Contenido, string Extension, string Mime)> ValidarAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length <= 0) throw new ApiException("Debe adjuntar una imagen no vacía.");
        if (file.Length > MaxTamano) throw new ApiException("La imagen supera 5 MiB.", StatusCodes.Status413PayloadTooLarge);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var expected = extension switch
        {
            ".jpg" or ".jpeg" => (SKEncodedImageFormat.Jpeg, "image/jpeg", ".jpg"),
            ".png" => (SKEncodedImageFormat.Png, "image/png", ".png"),
            ".webp" => (SKEncodedImageFormat.Webp, "image/webp", ".webp"),
            _ => throw new ApiException("Use una imagen JPG, PNG o WebP.")
        };
        if (!string.Equals(file.ContentType, expected.Item2, StringComparison.OrdinalIgnoreCase))
            throw new ApiException("El tipo de contenido no coincide con la imagen.");

        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int count;
        while ((count = await input.ReadAsync(chunk, ct)) != 0)
        {
            if (buffer.Length + count > MaxTamano)
                throw new ApiException("La imagen supera 5 MiB.", StatusCodes.Status413PayloadTooLarge);
            buffer.Write(chunk, 0, count);
        }
        var bytes = buffer.ToArray();
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        if (codec is null || codec.EncodedFormat != expected.Item1)
            throw new ApiException("El archivo no contiene una imagen válida del formato indicado.");
        var source = codec.Info;
        if (source.Width <= 0 || source.Height <= 0 || source.Width > 8192 || source.Height > 8192
            || (long)source.Width * source.Height > MaxPixeles || codec.FrameCount > 1)
            throw new ApiException("La imagen debe ser estática, de hasta 8192 píxeles por lado y 20 megapíxeles.");
        using var bitmap = new SKBitmap(new SKImageInfo(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
            throw new ApiException("La imagen está dañada o incompleta.");
        return (bytes, expected.Item3, expected.Item2);
    }
}
