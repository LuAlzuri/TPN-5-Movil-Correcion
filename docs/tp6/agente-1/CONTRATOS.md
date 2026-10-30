# Contratos de integración del Plan 1

Una solución `Apinventario.slnx`, con la API existente y una app MAUI Android para los tres roles. Los componentes y servicios del Plan 2 se agregan a **Apinventario.App**, no a otra aplicación. Backend de referencia: main f65e069, que incorpora auditoría del PR 28. Sin cambios de backend ni nuevas migraciones en esta implementación.

## Propiedad de archivos

Plan 1 mantiene csproj, MauiProgram, App/MainPage, Router, imports globales, Layout, Shared, Models/Common, Services/Infrastructure, configuración Android y estilos globales. También Pages/Acceso, Administracion, Auditoria y Reportes, Models/Administracion y Services/Administracion.

Plan 2 mantiene Pages/Tienda, Compras e Inventario, Models/Comercio y Services/Comercio; utiliza imports locales y estilos `.razor.css`. No editar el csproj o MauiProgram desde el Plan 2. Comunicar dependencias y una extensión `AddComercio(IServiceCollection)` al responsable de Plan 1 para registrar sus servicios. Mientras esa extensión no exista, no se invoca ni se sustituye por una implementación ficticia.

La ruta `/inventario` está implementada por Plan 1 en `Pages/Acceso/InicioPersonal.razor`; no duplicarla. Rutas acordadas con Plan 2: `/tienda`, `/tienda/productos/{id}`, `/compras/carrito`, `/compras/pago/{salidaId}`, `/compras/pedidos`, `/compras/pedidos/{id}`, `/inventario/productos`, `/inventario/productos/{id}`, `/inventario/proveedores`, `/inventario/ingresos`, `/inventario/ingresos/{id}`, `/inventario/solicitudes` y `/inventario/solicitudes/{id}`. Enlaces no implementados muestran pantalla no disponible; no se presentan como funcionales.

## Servicios comunes disponibles

Namespace `Apinventario.App.Services.Infrastructure`.

| Servicio | Contrato y comportamiento |
|---|---|
| SessionService | `CurrentUser`, `IsAuthenticated`, `ExpiresAtUtc`, evento `Changed`; `SignInAsync(email,password,ct)`, `RestoreAsync(ct)`, `SignOutAsync(ct)`. Validación con API; el temporizador vence la sesión. No guardar contraseñas. |
| ITokenStore | `ReadAsync()`, `WriteAsync(StoredSession)`, `Clear()`. Implementación TokenStore usa SecureStorage. Solo infraestructura debe acceder a ella. |
| ApiClient | `SendJsonAsync<T>(HttpMethod,string,object? body=null,CancellationToken ct=default,bool authenticated=true)`; `SendNoContentAsync(HttpMethod,string,object? body=null,CancellationToken ct=default)`; `SendMultipartAsync<T>(string,MultipartFormDataContent,CancellationToken)`; `DownloadAsync(string,CancellationToken)`. |
| ApiConfig | `BaseUri`, `ResolveImageUrl(string?)`. Una configuración de origen HTTP(S). `SetDevelopmentAddress(string)` solo Debug; persiste mediante el host. No conectar a SQL desde móvil. |
| ConfirmService | `ConfirmAsync(titulo,mensaje,confirmar="Confirmar",cancelar="Cancelar")` devuelve bool. Diálogo nativo accesible. `NotifyAsync(titulo,mensaje)` muestra resultado. |
| FileService | `PickImageAsync()` devuelve `FileResult?` (cancelar devuelve null); `OpenPdfAsync(DownloadedFile)` devuelve si se pudo abrir; `SharePdfAsync(DownloadedFile)` abre compartición nativa. El llamador abre el stream de la imagen elegida. |
| AppErrorLogger | `WriteAsync(operation,statusCode=null)` devuelve correlación. Etiquetas admitidas HTTP, Network, File, Session, UI. No acepta cuerpos ni excepciones sin sanitizar. Archivos privados rotados de aproximadamente 256 KiB por archivo y dos generaciones. |

Ejemplo de servicio del Plan 2:

```csharp
public sealed class ProductosClient(ApiClient api)
{
    public Task<PageResult<ProductoDto>> ListarAsync(int page, CancellationToken ct) =>
        api.SendJsonAsync<PageResult<ProductoDto>>(HttpMethod.Get,
            "api/productos?" + ApiClient.Query(("page", page), ("pageSize", 20)), ct: ct);
}
```

`ApiClient` adjunta Bearer únicamente a rutas relativas del origen configurado. No reintenta escrituras. 401 autenticado limpia token y sesión; 403 conserva sesión. Error de red, 5xx o timeout no se convierten en lista vacía. Los errores son `ApiFailure`: `StatusCode`, `Message`, `ValidationErrors`, `RetryAfter`. Un timeout de escritura tiene resultado incierto: consultar antes de repetir.

No pasar URLs absolutas a ApiClient. La descarga comprueba MIME y cabecera PDF; los bytes viajan al servicio nativo, no una URL con token. El transporte consume el HttpContent del multipart al terminar: no reutilizarlo en otra solicitud.

## Modelos comunes

Namespace `Apinventario.App.Models.Common`:

- `UserInfo`: Id int, Nombre, Apellido, CUIT, Email, Rol, Eliminado.
- `StoredSession`: Token y ExpiresAtUtc; nunca incluir en UI, logs o evidencias.
- `PageResult<T>`: Page, PageSize, TotalCount, TotalPages, HasNext, HasPrevious, Items. Paginación en servidor, desde 1 y máximo 100.
- `DownloadedFile`: Bytes, MimeType y FileName.
- `UiDates.Local(DateTime)`: trata DateTime SQL sin sufijo como UTC antes de convertir a zona local. DateOnly no se convierte a otra zona.

Auditoría de acciones usa `ActionPage` en Models/Administracion: Items, Total, Page, PageSize y TotalPages calculado. No deserializarla en PageResult ni convertir ids long de eventos a int.

## Componentes y estilo

`Feedback` recibe Busy, Error y Message. En Razor los parámetros string que provienen de variables se pasan con `@`: `<Feedback Busy="busy" Error="@error" Message="@message" />`.

`Pager` recibe Page, Pages, Total, Busy y EventCallback<int> Change. `RecordFields` es un visualizador de JSON histórico solo lectura. `LoggedErrorBoundary` registra una etiqueta sanitizada, sin excepción/cuerpo. La sesión y el acceso de rutas se exponen con AuthorizeView y `[Authorize(Roles="...")]`. También hay que manejar 403 de la API.

CSS global: `panel`, `tiles`, `actions`, `filters`, `table-wrap`, `detail-grid`, `badge-state`, `muted`, además de Bootstrap 5.3.3 empaquetado localmente con su licencia. Paleta blanco/gris con acento #0F766E. Tamaño táctil mínimo 48 px. No cargar Bootstrap o JS desde CDN. Formularios de Plan 2 agregan su propio control de borradores y confirmaciones de abandono cuando corresponda.

## Dispositivo y configuración

Debug usa `http://10.0.2.2:5087/`. Android permite HTTP únicamente para dominios del XML de desarrollo (emulador y loopback). Para teléfono físico con HTTP, el responsable del host debe añadir la IP LAN exacta al XML Debug; no hay excepción de subred ni TLS global deshabilitado. Preferir HTTPS con certificado confiable. Release bloquea HTTP y tiene un origen HTTPS deliberadamente no desplegable hasta elegir el servidor real.

La vista web temporal de QA está fuera del repositorio y usa los mismos componentes/servicios portables, con adaptadores web para sesión en memoria, archivos y confirmaciones. No modifica la solución entregable, no acredita SecureStorage o Android y no debe publicarse como sustituto del cliente Hybrid.
