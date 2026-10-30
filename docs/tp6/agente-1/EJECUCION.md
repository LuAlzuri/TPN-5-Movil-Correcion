# Ejecutar el frontend del TP6

La solución raíz `Apinventario.slnx` contiene API y app móvil. La solución de API `TPN°5-Movil.slnx` sigue disponible por separado. No hay otra app por rol.

## Herramientas comprobadas

- SDK .NET 10.0.401 para frontend net10.0-android.
- MAUI Controls y WebView.Maui 10.0.20 resueltos por el workload instalado.
- Microsoft.Android.Sdk.Windows 36.1.69 y plataforma android-36 instalada.
- Android SDK encontrado en `C:\Program Files (x86)\Android\android-sdk`; el entorno también apunta a C:\Android-SDK, que no tiene esa plataforma. Se usa un parámetro local en vez de cambiar ajustes globales.
- Bootstrap 5.3.3 local, con licencia MIT incluida.

## Comandos PowerShell

Desde la raíz:

```powershell
dotnet restore Apinventario.App/Apinventario.App.csproj
dotnet build Apinventario.App/Apinventario.App.csproj -f net10.0-android '-p:AndroidSdkDirectory=C:\Program Files (x86)\Android\android-sdk'
dotnet test tests/Apinventario.App.Unit/Apinventario.App.Unit.csproj
```

El SDK Android debe estar en una ruta válida de cada equipo; no copiar la ruta si no existe. Abrir Apinventario.slnx en el IDE también permite seleccionar el proyecto MAUI y un dispositivo. No es obligatorio compilar el móvil para ejecutar la API.

Iniciar API por separado, con la configuración local segura existente:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ASPNETCORE_URLS='http://127.0.0.1:5087'
dotnet run --project 'TPN°5-Movil/TPN°5-Movil.csproj' --no-launch-profile
```

No ejecutar migraciones ni crear cuentas de nuevo para cada arranque. No agregar passwords/JWT/SMTP al repositorio. Consultar al propietario por las cuentas de desarrollo disponibles.

## Red Android

Login tiene un desplegable Debug para origen API. En emulador Android estándar es `http://10.0.2.2:5087`. Las llamadas nativas no usan el localhost del teléfono para llegar a la PC. Para teléfono físico usar origen HTTPS accesible y confiable, o una IP LAN exacta incluida por el dueño del host en el XML de red Debug. Cualquier cambio de escucha/firewall requiere explicar alcance y autorización; no exponer SQL al móvil.

`network_security_config.xml` bloquea HTTP en Release. Debug tiene un recurso alternativo que admite solo emulador/loopback. Nunca se deshabilita validación de certificados. Cambiar servidor no es un cambio de endpoint ni del contrato HTTP.

El origen de Release está deliberadamente sin configurar (dominio .invalid). Antes de distribuir, reemplazarlo por HTTPS del servidor elegido. No presentar el APK Debug como despliegue productivo.

## Prueba de pantallas en navegador

La vista de desarrollo temporal está fuera del repo. Reutiliza componentes y servicios portables; adapta almacenamiento a memoria, confirmaciones al navegador y PDF a descarga. No pertenece a la solución entregable y no es una segunda app funcional del negocio. Se usa solo para comprobar visualmente pantallas mientras la prueba Android está detenida por indicación del propietario.

Esta vista no acredita el funcionamiento de SecureStorage, selector nativo, compartir/visor PDF ni teclado/Atrás de Android. No confundir una captura del navegador con evidencia Android. La consigna sigue exigiendo la comprobación final en dispositivo o emulador Android.

## Correo

La configuración local inspeccionada tiene SMTP deshabilitado y sin host. El formulario llama al endpoint real; no simula un éxito. Para completar la prueba se necesita configurar SMTP en servidor mediante secretos locales, con cuenta remitente y permisos adecuados. El destinatario de ensayo se ingresa en la UI; no debe quedar fijo en código. No entregar la contraseña de la cuenta personal al frontend.

## Reparto

Leer CONTRATOS.md antes de conectar el Plan 2. No crear otro host, HttpClient o sesión. El Plan 1 reserva /inventario como inicio; el Plan 2 agrega las pantallas funcionales en sus carpetas y comunica su registro de servicios al responsable del arranque.
