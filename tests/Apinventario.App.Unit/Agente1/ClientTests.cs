using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Apinventario.App.Models.Common;
using Apinventario.App.Models.Administracion;
using Apinventario.App.Services.Infrastructure;
using Apinventario.App.Services.Administracion;
using Xunit;

namespace Apinventario.App.Unit.Agente1;

public sealed class ClientTests
{
    private sealed class MemoryTokens : ITokenStore
    {
        public StoredSession? Value = new("private-test-token",DateTimeOffset.UtcNow.AddHours(1));
        public Task<StoredSession?> ReadAsync()=>Task.FromResult(Value);
        public Task WriteAsync(StoredSession value){Value=value;return Task.CompletedTask;}
        public void Clear()=>Value=null;
    }
    private sealed class Handler(Func<HttpRequestMessage,Task<HttpResponseMessage>> respond) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>respond(request); }
    private static HttpResponseMessage Json(object body,HttpStatusCode code=HttpStatusCode.OK)=>new(code){Content=new StringContent(JsonSerializer.Serialize(body),Encoding.UTF8,"application/json")};
    private sealed class Fixture : IDisposable
    {
        public readonly string Directory=Path.Combine(Path.GetTempPath(),"apinventario-tests",Guid.NewGuid().ToString("N"));
        public readonly MemoryTokens Tokens=new();
        public readonly ApiConfig Config=new("https://api.example.test");
        public readonly AppErrorLogger Logger;
        public readonly ApiClient Api;
        private readonly HttpClient http;
        public Fixture(Func<HttpRequestMessage,Task<HttpResponseMessage>> response)
        {Logger=new(Directory);http=new(new Handler(response));Api=new(http,Config,Tokens,Logger);}
        public void Dispose()=>http.Dispose();
    }
    [Fact] public async Task SendsBearerAndAccepts204WithoutReadingJson()
    {
        using var f=new Fixture(r=>{Assert.Equal("Bearer",r.Headers.Authorization?.Scheme);Assert.Equal("private-test-token",r.Headers.Authorization?.Parameter);Assert.Equal("/api/usuarios/4",r.RequestUri!.AbsolutePath);return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));});
        await f.Api.SendNoContentAsync(HttpMethod.Delete,"api/usuarios/4");
    }
    [Theory][InlineData(401,true)][InlineData(403,false)]
    public async Task Only401ClearsSession(int code,bool cleared)
    {
        using var f=new Fixture(_=>Task.FromResult(Json(new {error="denied"},(HttpStatusCode)code)));
        var triggered=false;f.Api.Unauthorized+=()=>triggered=true;
        var error=await Assert.ThrowsAsync<ApiFailure>(()=>f.Api.SendNoContentAsync(HttpMethod.Get,"api/test"));
        Assert.Equal(code,error.StatusCode);Assert.Equal(cleared,f.Tokens.Value is null);Assert.Equal(cleared,triggered);
    }
    [Fact] public async Task ExpiredTokenNeverReachesNetwork()
    {
        using var f=new Fixture(_=>throw new Exception("Must not send"));f.Tokens.Value=new("expired",DateTimeOffset.UtcNow.AddSeconds(-1));
        Assert.Equal(401,(await Assert.ThrowsAsync<ApiFailure>(()=>f.Api.SendNoContentAsync(HttpMethod.Get,"api/test"))).StatusCode);
    }
    [Fact] public async Task ValidationErrorsArePreservedAndLogsDoNotContainRequestOrResponseSecrets()
    {
        using var f=new Fixture(_=>Task.FromResult(Json(new {errors=new {Email=new[]{"Secret response data"}}},HttpStatusCode.BadRequest)));
        var error=await Assert.ThrowsAsync<ApiFailure>(()=>f.Api.SendNoContentAsync(HttpMethod.Put,"api/usuarios/4",new {password="secret-password"}));
        Assert.Equal("Secret response data",error.ValidationErrors["Email"][0]);
        var log=await File.ReadAllTextAsync(Path.Combine(f.Directory,"errors.jsonl"));
        Assert.DoesNotContain("secret",log,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("private-test-token",log);Assert.Contains("400",log);
    }
    [Fact] public async Task RateLimitPreservesRetryAfter()
    {
        using var f=new Fixture(_=>{var r=new HttpResponseMessage(HttpStatusCode.TooManyRequests);r.Headers.RetryAfter=new RetryConditionHeaderValue(TimeSpan.FromSeconds(37));return Task.FromResult(r);});
        Assert.Equal(TimeSpan.FromSeconds(37),(await Assert.ThrowsAsync<ApiFailure>(()=>f.Api.SendNoContentAsync(HttpMethod.Get,"api/test"))).RetryAfter);
    }
    [Fact] public async Task ActionsUseTotalAndLongIdentifiers()
    {
        using var f=new Fixture(_=>Task.FromResult(Json(new {items=new[]{new {id=4_000_000_000L,usuarioId=2,accion="Confirmar",entidad="Ingreso",fechaUtc=DateTime.UtcNow}},total=41,page=1,pageSize=20})));
        var page=await new AuditClient(f.Api).ActionsAsync(1,null,null,null,null,null);
        Assert.Equal(41,page.Total);Assert.Equal(3,page.TotalPages);Assert.Equal(4_000_000_000L,page.Items[0].Id);
    }
    [Fact] public async Task EmptyPasswordAndRoleAreOmittedFromAccountUpdate()
    {
        using var f=new Fixture(async r=>{using var body=JsonDocument.Parse(await r.Content!.ReadAsStringAsync());Assert.False(body.RootElement.TryGetProperty("password",out _));Assert.False(body.RootElement.TryGetProperty("rol",out _));return new(HttpStatusCode.NoContent);});
        await new AdminClient(f.Api).UpdateUserAsync(2,new(){Nombre="Demo",Apellido="User",CUIT="1",Email="demo@example.test",Rol="Administrador"});
    }
    [Fact] public async Task RegistrationSendsOnlyProfileFieldsAllowedForRole()
    {
        using var f=new Fixture(async r=>{using var body=JsonDocument.Parse(await r.Content!.ReadAsStringAsync());Assert.Equal(JsonValueKind.Null,body.RootElement.GetProperty("telefono").ValueKind);Assert.Equal("2026-10-29",body.RootElement.GetProperty("fechaIngreso").GetString());return Json(new UserInfo(2,"E","Demo","2","e@example.test","Empleado",false));});
        await new AdminClient(f.Api).CreateUserAsync(new(){Rol="Empleado",Password="test-password-123",FechaIngreso=new DateTime(2026,10,29),Telefono="must not send"});
    }
    [Fact] public async Task InvalidReportDatesNeverCallServer()
    {
        using var f=new Fixture(_=>throw new Exception("Must not send"));
        await Assert.ThrowsAsync<ApiFailure>(()=>new ReportsClient(f.Api).DownloadAsync(new(){Tipo="ingresos",Desde=new(2026,10,29),Hasta=new(2026,10,1)}));
    }
    [Fact] public async Task DownloadRequiresActualPdfHeader()
    {
        using var f=new Fixture(_=>{var r=new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("not PDF")};r.Content.Headers.ContentType=new("application/pdf");return Task.FromResult(r);});
        await Assert.ThrowsAsync<ApiFailure>(()=>f.Api.DownloadAsync("api/reportes/stock"));
    }
    [Theory][InlineData("https://external.test/api")][InlineData("//external.test/api")][InlineData("../secret")]
    public async Task NeverSendsBearerToAbsoluteOrTraversalPaths(string path)
    {
        using var f=new Fixture(_=>throw new Exception("Must not send"));await Assert.ThrowsAsync<ArgumentException>(()=>f.Api.SendNoContentAsync(HttpMethod.Get,path));
    }
    [Theory][InlineData("https://user:password@api.example.test")][InlineData("http://public.example.test")][InlineData("https://api.example.test/?token=secret")]
    public void BaseAddressRejectsCredentialsAndUnsafeHttp(string value)=>Assert.Throws<ArgumentException>(()=>ApiConfig.Validate(value,true));
    [Fact] public void AuditDayBoundaryIsExplicitUtcMinusThree()
    {
        Assert.Equal("2026-10-29T03:00:00.0000000Z",AuditClient.Start(new DateTime(2026,10,29)));
        Assert.Equal("2026-10-30T02:59:59.9999999Z",AuditClient.End(new DateTime(2026,10,29)));
    }
    [Fact] public async Task RestoreUsesServerProfileAndLogoutClearsIt()
    {
        using var f=new Fixture(r=>{Assert.Equal("/api/auth/me",r.RequestUri!.AbsolutePath);return Task.FromResult(Json(new UserInfo(1,"A","Demo","1","a@example.test","Administrador",false)));});
        using var session=new SessionService(f.Api,f.Tokens);await session.RestoreAsync();Assert.True(session.IsAuthenticated);Assert.Equal("Administrador",session.CurrentUser!.Rol);
        await session.SignOutAsync();Assert.False(session.IsAuthenticated);Assert.Null(f.Tokens.Value);
    }
    [Fact] public async Task LoginDoesNotSendOldTokenAndStoresNewSession()
    {
        using var f=new Fixture(r=>{Assert.Null(r.Headers.Authorization);return Task.FromResult(Json(new LoginResult("new-token",DateTime.UtcNow.AddHours(1),new UserInfo(1,"A","Demo","1","a@example.test","Administrador",false))));});
        using var session=new SessionService(f.Api,f.Tokens);await session.SignInAsync("a@example.test","test-password");Assert.Equal("new-token",f.Tokens.Value!.Token);Assert.True(session.IsAuthenticated);
    }
    [Fact] public async Task ServerFailureDoesNotDiscloseInternalDetails()
    {
        using var f=new Fixture(_=>Task.FromResult(Json(new {error="SQL password=secret"},HttpStatusCode.InternalServerError)));
        var error=await Assert.ThrowsAsync<ApiFailure>(()=>f.Api.SendNoContentAsync(HttpMethod.Get,"api/test"));Assert.DoesNotContain("secret",error.Message);
    }
}
