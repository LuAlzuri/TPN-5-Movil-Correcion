using Apinventario.App.Models.Common;

namespace Apinventario.App.Services.Infrastructure;

public sealed class ConfirmService
{
    public Task<bool> ConfirmAsync(string titulo, string mensaje, string confirmar = "Confirmar", string cancelar = "Cancelar") =>
        MainThread.InvokeOnMainThreadAsync(() => Page().DisplayAlertAsync(titulo, mensaje, confirmar, cancelar));
    public Task NotifyAsync(string titulo, string mensaje) => MainThread.InvokeOnMainThreadAsync(() => Page().DisplayAlertAsync(titulo, mensaje, "Aceptar"));
    private static Page Page() => Application.Current?.Windows.FirstOrDefault()?.Page ?? throw new InvalidOperationException("No hay una ventana disponible.");
}
public sealed class FileService
{
    public Task<FileResult?> PickImageAsync() => MainThread.InvokeOnMainThreadAsync(() => FilePicker.Default.PickAsync(new PickOptions
    { PickerTitle = "Seleccionar imagen", FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>> { [DevicePlatform.Android] = ["image/jpeg", "image/png", "image/webp"] }) }));
    private static async Task<string> SaveAsync(DownloadedFile file)
    {
        var directory = Path.Combine(FileSystem.CacheDirectory, "reports");
        Directory.CreateDirectory(directory);
        foreach (var old in Directory.EnumerateFiles(directory).Where(x => File.GetLastWriteTimeUtc(x) < DateTime.UtcNow.AddDays(-1))) File.Delete(old);
        var name = Guid.NewGuid().ToString("N") + "-" + Path.GetFileName(file.FileName);
        var path = Path.Combine(directory, name);
        await File.WriteAllBytesAsync(path, file.Bytes);
        return path;
    }
    public async Task<bool> OpenPdfAsync(DownloadedFile file) => await Launcher.Default.OpenAsync(new OpenFileRequest("Reporte", new ReadOnlyFile(await SaveAsync(file), "application/pdf")));
    public async Task SharePdfAsync(DownloadedFile file) => await Share.Default.RequestAsync(new ShareFileRequest("Compartir reporte", new ShareFile(await SaveAsync(file), "application/pdf")));
}
