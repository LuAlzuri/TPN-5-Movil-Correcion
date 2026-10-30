using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Apinventario.App.Services.Infrastructure;
namespace Apinventario.App.Components.Shared;
public sealed class LoggedErrorBoundary : ErrorBoundary
{
    [Inject] public AppErrorLogger Logger { get; set; } = default!;
    protected override async Task OnErrorAsync(Exception exception) => await Logger.WriteAsync("UI");
}
