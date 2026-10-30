using System.Text.Json;

namespace Apinventario.App.Services.Infrastructure;

public sealed class AppErrorLogger(string directory)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public async Task<string> WriteAsync(string operation, int? statusCode = null)
    {
        var correlation = Guid.NewGuid().ToString("N");
        try
        {
            await gate.WaitAsync();
            try
            {
                Directory.CreateDirectory(directory);
                var file = Path.Combine(directory, "errors.jsonl");
                if (File.Exists(file) && new FileInfo(file).Length > 256 * 1024)
                    File.Move(file, Path.Combine(directory, "errors.previous.jsonl"), true);
                // Only a controlled operation label, status and correlation; no exception/body/URL.
                var label = operation is "HTTP" or "Network" or "File" or "Session" or "UI" ? operation : "UI";
                await File.AppendAllTextAsync(file, JsonSerializer.Serialize(new
                { utc = DateTimeOffset.UtcNow, operation = label, statusCode, correlation }) + Environment.NewLine);
            }
            finally { gate.Release(); }
        }
        catch { /* A diagnostic failure must not prevent use of the app. */ }
        return correlation;
    }
}
