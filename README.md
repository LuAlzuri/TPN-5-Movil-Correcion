# Apinventario

## Información general

Apinventario es una aplicación de gestión de inventario y ventas para un negocio, desarrollada para los trabajos prácticos de Programación Móvil. Su alcance comprende catálogo de productos, carrito de compras, pago simulado, entradas y salidas de mercadería, control de stock, reportes PDF con envío por correo, auditoría y registro de errores.

El repositorio reúne una API REST en ASP.NET Core y una aplicación móvil .NET MAUI Blazor Hybrid orientada a Android. La solución `Apinventario.slnx` agrupa el backend, la app y las pruebas del frontend. Los roles **Administrador**, **Empleado** y **Cliente** utilizan la misma app, con acceso a las funciones correspondientes a cada perfil. La validación móvil se realiza en un emulador Android.

## Tecnologías utilizadas

| Tecnología | Versión o referencia | Uso |
|---|---|---|
| .NET y ASP.NET Core | Backend `net9.0` | API REST y servicios de negocio |
| .NET MAUI y Blazor Hybrid | Frontend `net10.0-android`; MAUI 10.0.20 en la restauración verificada | Aplicación Android con componentes Razor dentro de un WebView |
| .NET SDK | 10.0.401 utilizado para compilar el frontend | Herramientas de desarrollo y compilación |
| Entity Framework Core | 9.0.11 | Persistencia y migraciones Code First |
| SQL Server | Motor configurado en el entorno | Base de datos relacional |
| Bootstrap | 5.3.3, incluido localmente | Estilos y distribución de las pantallas |
| Swagger / OpenAPI | Swashbuckle.AspNetCore 7.3.1 | Descripción y consulta de la API |
| QuestPDF | 2026.9.1 | Generación de reportes PDF |
| Serilog | Paquetes detallados debajo | Registro de errores del backend |
| SMTP / System.Net.Mail | Incluido en .NET | Envío de reportes adjuntos desde el backend |
| xUnit y Testcontainers | Paquetes detallados debajo | Pruebas unitarias e integración con SQL Server descartable en Docker |
| Git | Control de versiones | Trabajo conjunto en un único repositorio |

### Paquetes NuGet del backend

Versiones de las referencias directas declaradas en `TPN°5-Movil.csproj`.

| Paquete | Versión |
|---|---|
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.11 |
| Microsoft.AspNetCore.OpenApi | 9.0.15 |
| Microsoft.EntityFrameworkCore.Design | 9.0.11 |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.11 |
| Microsoft.EntityFrameworkCore.Tools | 9.0.11 |
| QuestPDF | 2026.9.1 |
| Serilog.Extensions.Logging | 9.0.0 |
| Serilog.Sinks.File | 7.0.0 |
| Swashbuckle.AspNetCore | 7.3.1 |
| SkiaSharp | 4.153.1 |
| SkiaSharp.NativeAssets.Linux.NoDependencies | 4.153.1; referencia condicional para Linux |

### Paquetes NuGet de la app móvil

| Paquete | Versión |
|---|---|
| Microsoft.Maui.Controls | `$(MauiVersion)`; 10.0.20 en la restauración verificada |
| Microsoft.AspNetCore.Components.WebView.Maui | `$(MauiVersion)`; 10.0.20 en la restauración verificada |
| Microsoft.Extensions.Logging.Debug | 10.0.0 |
| Microsoft.AspNetCore.Components.Authorization | 10.0.0 |

`MauiVersion` procede del workload MAUI instalado. Bootstrap es un recurso CSS local, no un paquete NuGet de la app.

### Paquetes NuGet de pruebas

| Paquete | Versión | Proyecto |
|---|---|---|
| Microsoft.NET.Test.Sdk | 17.12.0 | Pruebas de backend y frontend |
| xunit | 2.9.2 | Pruebas de backend y frontend |
| xunit.runner.visualstudio | 2.8.2 | Pruebas de backend y frontend |
| Microsoft.AspNetCore.Mvc.Testing | 9.0.11 | Integración del backend |
| Testcontainers.MsSql | 4.16.0 | Integración del backend |
| PdfPig | 0.1.16 | Verificación de los PDF del backend |

La herramienta de comprobación del esquema también referencia `Microsoft.EntityFrameworkCore.SqlServer` y `Microsoft.EntityFrameworkCore.Design` 9.0.11. Las tablas enumeran dependencias directas; las dependencias transitivas se resuelven durante la restauración de NuGet.

## Uso de JWT

La API utiliza **JSON Web Tokens (JWT)** para autenticar las cuentas y autorizar operaciones según su rol. El inicio de sesión se realiza mediante `POST /api/auth/login`, enviando email y contraseña. Si las credenciales son válidas, la API devuelve el token, su vencimiento y los datos del usuario.

El token está firmado con **HS256 (HMAC-SHA256)** e incluye identificador de usuario, nombre, email, rol, identificador de token y versión de credenciales. Para acceder a los endpoints protegidos, la app envía el token en la cabecera:

```http
Authorization: Bearer <token>
```

El backend comprueba firma, emisor, audiencia y vencimiento, con una tolerancia de reloj de 30 segundos. También verifica que la cuenta y su perfil sigan disponibles y que la versión de credenciales coincida. Los permisos se aplican en el servidor para los roles **Administrador**, **Empleado** y **Cliente**.

La configuración utiliza `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key` y `Jwt:ExpireMinutes`. La clave debe tener al menos 32 bytes UTF-8. La duración predeterminada es de 120 minutos y admite valores entre 1 y 1440 minutos. La clave de firma pertenece únicamente al backend y se suministra mediante configuración local segura; no se incluye en el frontend ni en Git.

En la app MAUI, el token y su vencimiento se guardan en **SecureStorage**; la contraseña no se conserva. Al restaurar una sesión, se valida la cuenta mediante `GET /api/auth/me`. El cierre de sesión elimina los datos locales de autenticación. Una respuesta 401 en una solicitud autenticada o el vencimiento del token obliga a iniciar sesión nuevamente; una respuesta 403 indica falta de permisos y conserva la sesión válida. No hay renovación automática mediante refresh tokens.
