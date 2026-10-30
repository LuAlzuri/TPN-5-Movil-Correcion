using System.Security.Claims;
using Apinventario.App.Models.Common;
using Microsoft.AspNetCore.Components.Authorization;

namespace Apinventario.App.Services.Infrastructure;

public sealed class SessionService : IDisposable
{
    private readonly ApiClient api;
    private readonly ITokenStore tokens;
    private System.Threading.Timer? expiry;
    public UserInfo? CurrentUser { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null && ExpiresAtUtc > DateTimeOffset.UtcNow;
    public event Action? Changed;
    public SessionService(ApiClient api, ITokenStore tokens)
    { this.api = api; this.tokens = tokens; api.Unauthorized += Clear; }
    public async Task SignInAsync(string email, string password, CancellationToken ct = default)
    {
        Clear();
        var result = await api.SendJsonAsync<LoginResult>(HttpMethod.Post, "api/auth/login", new { email, password }, ct, false);
        if (result.Usuario.Eliminado || result.Usuario.Rol is not ("Administrador" or "Empleado" or "Cliente"))
            throw new ApiFailure(403, "La cuenta no tiene un rol habilitado.");
        var expires = new DateTimeOffset(DateTime.SpecifyKind(result.ExpiresAt, DateTimeKind.Utc));
        if (expires <= DateTimeOffset.UtcNow) throw new ApiFailure(401, "La sesión recibida ya venció.");
        await tokens.WriteAsync(new(result.Token, expires));
        Set(result.Usuario, expires);
    }
    public async Task RestoreAsync(CancellationToken ct = default)
    {
        var saved = await tokens.ReadAsync();
        if (saved is null || saved.ExpiresAtUtc <= DateTimeOffset.UtcNow) { Clear(); return; }
        try
        {
            var user = await api.SendJsonAsync<UserInfo>(HttpMethod.Get, "api/auth/me", ct: ct);
            if (user.Eliminado || user.Rol is not ("Administrador" or "Empleado" or "Cliente")) { Clear(); return; }
            Set(user, saved.ExpiresAtUtc);
        }
        catch (ApiFailure ex) when (ex.StatusCode == 401) { Clear(); }
    }
    private void Set(UserInfo user, DateTimeOffset expires)
    {
        if (expires <= DateTimeOffset.UtcNow) { Clear(); return; }
        CurrentUser = user; ExpiresAtUtc = expires;
        expiry?.Dispose();
        expiry = new System.Threading.Timer(_ => Clear(), null, expires - DateTimeOffset.UtcNow, Timeout.InfiniteTimeSpan);
        Changed?.Invoke();
    }
    public Task SignOutAsync(CancellationToken ct = default) { Clear(); return Task.CompletedTask; }
    private void Clear()
    { tokens.Clear(); CurrentUser = null; ExpiresAtUtc = default; expiry?.Dispose(); expiry = null; Changed?.Invoke(); }
    public void Dispose() { api.Unauthorized -= Clear; expiry?.Dispose(); }
}
public sealed class SessionAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly SessionService session;
    public SessionAuthenticationStateProvider(SessionService session) { this.session = session; session.Changed += Changed; }
    private void Changed() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = session.CurrentUser;
        var identity = !session.IsAuthenticated || user is null ? new ClaimsIdentity() : new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.Nombre + " " + user.Apellido),
            new Claim(ClaimTypes.Role, user.Rol)], "JWT");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }
    public void Dispose() => session.Changed -= Changed;
}
