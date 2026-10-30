using System.Text.Json.Serialization;
using Apinventario.App.Models.Administracion;
using Apinventario.App.Models.Common;
using Apinventario.App.Services.Infrastructure;

namespace Apinventario.App.Services.Administracion;

public sealed class AdminClient(ApiClient api)
{
    public Task<PageResult<UserInfo>> UsersAsync(int page, string? search, CancellationToken ct = default) =>
        api.SendJsonAsync<PageResult<UserInfo>>(HttpMethod.Get, "api/usuarios?" + ApiClient.Query(("page",page),("pageSize",20),("busqueda",search)), ct:ct);
    public Task<UserInfo> UserAsync(int id, CancellationToken ct = default) => api.SendJsonAsync<UserInfo>(HttpMethod.Get,$"api/usuarios/{id}",ct:ct);
    public Task<UserInfo> CreateUserAsync(UserForm form, CancellationToken ct = default)
    {
        if (form.Password.Length is < 12 or > 128) throw new ApiFailure(400,"La contraseña debe tener entre 12 y 128 caracteres.");
        if (form.Rol == "Empleado" && form.FechaIngreso is null) throw new ApiFailure(400,"Indicá la fecha de ingreso del empleado.");
        return api.SendJsonAsync<UserInfo>(HttpMethod.Post,"api/auth/register",new
        {
            form.Nombre,form.Apellido,form.CUIT,form.Email,form.Password,form.Rol,
            fechaIngreso=form.Rol == "Empleado" && form.FechaIngreso.HasValue ? DateOnly.FromDateTime(form.FechaIngreso.Value) : (DateOnly?)null,
            telefono=form.Rol == "Cliente" ? form.Telefono : null, direccion=form.Rol == "Cliente" ? form.Direccion : null
        },ct);
    }
    public Task UpdateUserAsync(int id, UserForm form, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(form.Password) && form.Password.Length is < 12 or > 128) throw new ApiFailure(400,"La contraseña debe tener entre 12 y 128 caracteres.");
        return api.SendNoContentAsync(HttpMethod.Put,$"api/usuarios/{id}",new UserUpdate(form.Nombre,form.Apellido,form.CUIT,form.Email,string.IsNullOrEmpty(form.Password)?null:form.Password),ct);
    }
    public Task DeleteUserAsync(int id, CancellationToken ct = default) => api.SendNoContentAsync(HttpMethod.Delete,$"api/usuarios/{id}",ct:ct);
    public Task<EmployeeProfile> EmployeeAsync(int id, CancellationToken ct = default) => api.SendJsonAsync<EmployeeProfile>(HttpMethod.Get,$"api/empleados/{id}",ct:ct);
    public Task<ClientProfile> ClientAsync(int id, CancellationToken ct = default) => api.SendJsonAsync<ClientProfile>(HttpMethod.Get,$"api/clientes/{id}",ct:ct);
    public Task UpdateProfileAsync(int id, UserForm form, CancellationToken ct = default)
    {
        if(form.Rol == "Empleado")
        {
            if(form.FechaIngreso is null) throw new ApiFailure(400,"Indicá la fecha de ingreso.");
            return api.SendNoContentAsync(HttpMethod.Put,$"api/empleados/{id}",new {form.Nombre,form.Apellido,form.CUIT,form.Email,fechaIngreso=DateOnly.FromDateTime(form.FechaIngreso.Value)},ct);
        }
        return api.SendNoContentAsync(HttpMethod.Put,$"api/clientes/{id}",new {form.Nombre,form.Apellido,form.CUIT,form.Email,form.Telefono,form.Direccion},ct);
    }
    public Task<PageResult<Category>> CategoriesAsync(int page,string? search,CancellationToken ct=default) => api.SendJsonAsync<PageResult<Category>>(HttpMethod.Get,"api/categorias?"+ApiClient.Query(("page",page),("pageSize",20),("busqueda",search)),ct:ct);
    public Task<Category> CategoryAsync(int id,CancellationToken ct=default) => api.SendJsonAsync<Category>(HttpMethod.Get,$"api/categorias/{id}",ct:ct);
    public Task<Category> CreateCategoryAsync(CategoryForm form,CancellationToken ct=default) => api.SendJsonAsync<Category>(HttpMethod.Post,"api/categorias",form,ct);
    public Task UpdateCategoryAsync(int id,CategoryForm form,CancellationToken ct=default) => api.SendNoContentAsync(HttpMethod.Put,$"api/categorias/{id}",form,ct);
    public Task DeleteCategoryAsync(int id,CancellationToken ct=default) => api.SendNoContentAsync(HttpMethod.Delete,$"api/categorias/{id}",ct:ct);
    private sealed record UserUpdate(string Nombre,string Apellido,string CUIT,string Email,
        [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)]string? Password);
}
