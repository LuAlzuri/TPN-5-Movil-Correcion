# Apinventario — API TPN 5 Móvil

API REST para administrar el inventario de un negocio: catálogo de productos, carrito de compras, solicitudes de venta, entradas de mercadería y stock actualizado. Implementada con ASP.NET Core, Entity Framework Core y SQL Server.

Este repositorio contiene el **backend**. Las vistas móviles con Blazor Hybrid para Android están planificadas; no se incluye aquí una aplicación móvil implementada.

La [documentación del proyecto](docs/DOCUMENTACION-PROYECTO.md) reúne el modelo, los contratos, las reglas, las migraciones, las verificaciones y el historial de decisiones.

## Funcionalidades

- Autenticación JWT y autorización con tres roles: **Administrador**, **Empleado** y **Cliente**.
- Gestión de usuarios y perfiles, categorías, productos y proveedores.
- Catálogo con búsqueda y paginado; una imagen por producto.
- Ingresos en borrador y confirmación que incorpora unidades al stock.
- Carrito propio por Cliente, envío de pedidos y confirmación por personal.
- Transacciones y actualizaciones condicionadas para proteger el stock ante concurrencia.
- Borrado lógico y consultas administrativas de auditoría.
- Consulta de pedidos cancelados por su Cliente propietario, en modo de solo lectura.
- Protección del último Administrador y límite de intentos de login.
- Swagger/OpenAPI y pruebas automatizadas con SQL Server descartable en Docker.

## Tecnologías y requisitos

| Componente | Uso / versión |
| --- | --- |
| .NET SDK | 9.0.3xx o compatible con `net9.0` y soluciones `.slnx`; verificado con 9.0.320 |
| ASP.NET Core | API, autenticación y autorización JWT |
| Entity Framework Core | 9.0.11, proveedor SQL Server |
| SQL Server | Express, Developer o instancia compatible; pruebas con SQL Server 2022 |
| `dotnet-ef` | 9.0.11 para administrar migraciones |
| Swagger | Swashbuckle.AspNetCore 7.3.1 |
| SkiaSharp | Validación del contenido real de imágenes |
| xUnit + Testcontainers | Pruebas unitarias e integración con SQL Server real |
| Docker | Necesario para las pruebas de integración; debe estar iniciado y poder ejecutar contenedores Linux |

Docker no es obligatorio para ejecutar la API si se usa SQL Server instalado localmente. Las pruebas sí lo requieren y descargan su propia imagen de SQL Server; no usan una instancia personal como alternativa. Este README no presupone un Dockerfile ni un Docker Compose de la aplicación.

## Estructura del repositorio

```text
TPN°5-Movil/
  Controllers/       Endpoints y permisos por acción
  DTOs/              Contratos HTTP, normalización y paginado
  Entities/          Entidades de dominio
  Data/              DbContext y utilidades de persistencia
  Services/          Autenticación, movimientos, importes, auditoría e imágenes
  Security/          JWT, identidad autenticada, grupos de roles y límite de login
  Exceptions/        Errores de negocio y estados HTTP
  Migrations/        Migraciones y snapshot del modelo
  wwwroot/uploads/   Imágenes de ejecución; excluidas de Git
  Program.cs         Configuración, middleware y bootstrap explícito
  appsettings.example.json
 tests/TPN5.Integration/  Pruebas unitarias e integración
 tools/Schema/           Verificación del modelo y herramientas de diseño
 docs/DOCUMENTACION-PROYECTO.md
 TPN°5-Movil.slnx
```

## Instalación y configuración local

Los siguientes comandos se ejecutan desde la raíz del repositorio en **Bash / Git Bash**. En PowerShell, definir variables con `$env:Nombre = 'valor'` en lugar de `export`.

```bash
git clone https://github.com/LuAlzuri/TPN-5-Movil-Correcion.git
cd TPN-5-Movil-Correcion
dotnet restore
dotnet build
```

### 1. Configurar SQL Server y JWT

La API usa `ConnectionStrings:DefaultConnection`. Para SQL Server Express en Windows con autenticación integrada:

```bash
export ConnectionStrings__DefaultConnection='Server=localhost\SQLEXPRESS;Database=TPN5;Trusted_Connection=True;TrustServerCertificate=True'
export Jwt__Issuer='Apinventario-Local'
export Jwt__Audience='Apinventario-Local'
export Jwt__ExpireMinutes='120'
```

Para Developer con instancia predeterminada, usar `Server=localhost`. Si se usa autenticación SQL, reemplazar la conexión por una con `User Id` y `Password`, suministrando las credenciales mediante configuración local segura. La autenticación integrada de este ejemplo requiere un entorno Windows compatible.

Definir una clave JWT aleatoria de al menos **32 bytes**. Por ejemplo, si OpenSSL está disponible:

```bash
export Jwt__Key="$(openssl rand -base64 48)"
```

Mantener estas variables en la misma terminal durante los pasos siguientes. Para volver a iniciar después, configurar los valores nuevamente o guardarlos con un mecanismo de secretos local. Cambiar la clave invalida los tokens emitidos anteriormente.

`appsettings.example.json` sirve de referencia. Las variables de entorno sobrescriben los valores de los archivos de configuración, incluidos los de desarrollo. No reutilizar claves de ejemplo o desarrollo para un despliegue ni subir claves, contraseñas o conexiones reales a Git. `TrustServerCertificate=True` se muestra para desarrollo local.

### 2. Crear la base de datos

El proyecto sigue **Code First** y contiene **14 migraciones existentes**. Para una base vacía, aplicarlas; no generar otra migración inicial.

Instalar la herramienta compatible en una carpeta propia, fuera del repositorio:

```bash
dotnet tool install dotnet-ef --version 9.0.11 --tool-path "$HOME/.dotnet/tpn5-ef9"
"$HOME/.dotnet/tpn5-ef9/dotnet-ef" --version
"$HOME/.dotnet/tpn5-ef9/dotnet-ef" database update --project 'TPN°5-Movil/TPN°5-Movil.csproj'
```

La instalación se realiza una sola vez. Si esa carpeta ya tiene la herramienta, comprobar su versión antes de usarla.

**Comprobar servidor y nombre de base en la conexión antes de aplicar las migraciones.** SQL Server debe estar disponible y la cuenta de conexión necesita permisos suficientes. El arranque normal de la API no crea la base, no aplica migraciones ni carga datos automáticamente.

El archivo histórico `TPN°5-Movil/dotnet-tools.json` apunta a EF 10. No usarlo para restaurar herramientas de este proyecto sin revisar esa diferencia: el esquema actual utiliza EF Core 9.0.11.

### 3. Crear el primer Administrador

Con la base migrada y la configuración anterior disponible:

```bash
export Bootstrap__Nombre='Administrador'
export Bootstrap__Apellido='Inicial'
export Bootstrap__CUIT='1'
export Bootstrap__Email='admin@example.test'
read -r -s -p 'Contraseña inicial (12 a 128 caracteres): ' Bootstrap__Password
export Bootstrap__Password
dotnet run --project 'TPN°5-Movil/TPN°5-Movil.csproj' --no-launch-profile -- --crear-administrador
unset Bootstrap__Password
```

El comando crea la cuenta y termina sin iniciar el servidor HTTP. No tiene contraseña predeterminada ni reemplaza un Administrador existente. Si ya hubo una cuenta Administrador, incluso eliminada, este mecanismo no permite crear otra inicial; las siguientes cuentas se administran mediante operaciones autenticadas.

### 4. Iniciar la API y abrir Swagger

```bash
export ASPNETCORE_ENVIRONMENT='Development'
export ASPNETCORE_URLS='http://127.0.0.1:5087'
dotnet run --project 'TPN°5-Movil/TPN°5-Movil.csproj' --no-launch-profile
```

- Swagger UI: <http://127.0.0.1:5087/swagger>
- Contrato OpenAPI: <http://127.0.0.1:5087/swagger/v1/swagger.json>
- Base de endpoints: `http://127.0.0.1:5087/api`

Sin `--no-launch-profile`, los perfiles del proyecto usan HTTP 5156 y HTTPS 7247. Los puertos anteriores son una elección explícita para estas instrucciones. En un emulador Android, `localhost` corresponde al dispositivo; configurar la dirección de la máquina que sirve la API según el emulador y la red utilizados.

## Autenticación y cuentas

1. Ejecutar `POST /api/auth/login` con `email` y `password`.
2. Usar el `token` devuelto en la cabecera `Authorization: Bearer <token>` de las solicitudes protegidas.
3. En Swagger, abrir **Authorize** e introducir el token según el campo de autenticación Bearer.
4. `GET /api/auth/me` devuelve los datos de la cuenta autenticada.

El registro actual **requiere Administrador**: `POST /api/auth/register` o los endpoints administrativos de cuentas/perfiles. No hay registro público, recuperación de contraseña ni edición de rol de una cuenta existente.

Cada Usuario tiene un único rol. Cliente y Empleado tienen un subperfil con la misma clave primaria; Administrador no tiene subperfil. El alta de cuenta y perfil es atómica. Los IDs de Cliente y del confirmador se obtienen del token, no se aceptan como identidad declarada en el JSON de movimientos.

Los tokens se comprueban también contra el estado de la cuenta: una cuenta eliminada, un perfil inválido o un cambio de contraseña pueden invalidar el acceso antes de que expire el JWT.

### Permisos principales

| Operación | Administrador | Empleado | Cliente |
| --- | --- | --- | --- |
| Administrar usuarios, clientes y empleados | Sí | No | No |
| CRUD de categorías | Sí | No | No |
| Leer catálogo de categorías y productos | Sí | Sí | Sí |
| CRUD de productos y proveedores | Sí | Sí | No |
| Crear ingresos y administrar sus detalles | No | Sí | No |
| Consultar ingresos operativos | Sí | Sí | No |
| Crear carrito y administrar sus detalles | No | No | Solo propios |
| Enviar o cancelar pedido antes de confirmación operativa | No | No | Solo propios |
| Confirmar venta solicitada | Sí | Sí | No |
| Leer cancelados mediante rutas operativas de salidas | No | No | Solo propios |
| Consultar auditoría e históricos administrativos | Sí | No | No |

Administrador no hereda todas las operaciones de Empleado o Cliente. La matriz detallada y los controles de cada ruta están en la documentación del proyecto.

El login admite por defecto **5 solicitudes por minuto por IP**, sin cola. El exceso devuelve `429` y `Retry-After`. Se configura con `LoginRateLimit__PermitLimit` y `LoginRateLimit__WindowSeconds`. La política CORS actual permite cualquier origen, método y cabecera.

## Flujo de inventario y ventas

### Entrada de mercadería

1. Empleado crea un encabezado con proveedor y fecha registrada por la API.
2. Agrega productos, cantidades y descuento porcentual mientras el ingreso está en borrador.
3. Confirma el ingreso: aumenta el stock una sola vez y bloquea los detalles.
4. El encabezado no se edita. Eliminar un ingreso confirmado revierte sus unidades o rechaza toda la operación si dejaría stock negativo.

### Catálogo, carrito y venta

1. Cliente consulta productos y crea un encabezado de salida para su carrito.
2. Agrega o ajusta detalles: una línea por producto y cantidad total entre **0 y el stock disponible**. Cero retira la línea; una cantidad negativa se rechaza.
3. Envía el pedido: `confirmarXCliente=true` y detalles bloqueados.
4. Administrador o Empleado confirma: registra su ID y descuenta stock en una transacción. Una venta puede dejar stock en **cero**, nunca negativo.
5. El Cliente puede cancelar antes de la confirmación operativa, incluso después de enviar el pedido. Luego de la confirmación del personal, la cancelación queda bloqueada.

El carrito **no reserva stock**. La disponibilidad se verifica al agregar, editar, solicitar y confirmar. Si una operación falla, no deben persistir descuentos parciales de stock.

Los precios se capturan al agregar cada detalle y se conservan como históricos. Descuentos: 0 a 100, con hasta dos decimales. El subtotal se redondea por detalle a dos decimales con `AwayFromZero`; el total suma esos subtotales y respeta `decimal(18,2)`.

### Borrado lógico y estados visuales

Los registros con `Eliminado` se ocultan mediante filtros globales, salvo consultas expresamente habilitadas. No se borran físicamente perfiles ni referencias históricas.

El Cliente puede consultar sus propias salidas canceladas en el listado, encabezado y detalle. `SalidaResponse` incluye `eliminado`; otro Cliente no puede acceder y los cancelados no pueden modificarse o confirmarse. El personal conserva sus consultas operativas sin eliminados y el Administrador dispone de auditoría.

| Condición de salida | Etiqueta posible en el front |
| --- | --- |
| `eliminado=true` | Cancelado |
| No eliminada y `confirmarXEmpleado=true` | Aceptado |
| No eliminada, confirmada por Cliente y aún no por personal | Pendiente |
| No eliminada y sin confirmaciones | En carrito |

Estas etiquetas son visuales; no se agrega una columna de estado textual. La unicidad de email incluye cuentas eliminadas. CUIT/CUIL se reserva según las reglas de Cliente y Proveedor, sin extender esa unicidad a todos los roles.

## Endpoints

La API tiene **74 combinaciones de método HTTP y ruta**, distribuidas en diez controladores. Swagger muestra contratos y parámetros; el comportamiento de negocio y los códigos posibles se detallan además en la documentación del proyecto.

| Recurso | Ruta base | Cantidad |
| --- | --- | ---: |
| Autenticación | `/api/auth` | 3 |
| Usuarios | `/api/usuarios` | 5 |
| Empleados | `/api/empleados` | 5 |
| Clientes | `/api/clientes` | 6 |
| Categorías | `/api/categorias` | 6 |
| Proveedores | `/api/proveedores` | 6 |
| Productos e imagen | `/api/productos` | 7 |
| Ingresos y detalles | `/api/ingresos` | 9 |
| Salidas y detalles | `/api/salidas` | 10 |
| Auditoría | `/api/auditoria` | 17 |
| **Total** | | **74** |

Los listados paginados comunes usan `page` y `pageSize`, con 1 y 10 por defecto y hasta 100 registros por página. Rechazan parámetros inválidos o desplazamientos fuera de rango con `400`; no los corrigen silenciosamente. Los filtros adicionales dependen del recurso.

Respuestas habituales: `200` consulta/actualización con cuerpo, `201` creación, `204` operación sin cuerpo, `400` validación, `401` autenticación, `403` permisos, `404` recurso inexistente o no visible, `409` conflicto, `413` imagen demasiado grande y `429` límite de login. La respuesta exacta depende de la acción.

## Imágenes de productos

`POST /api/productos/{id}/imagen` recibe `multipart/form-data`, campo `file`.

- Una imagen por producto; una carga posterior la reemplaza.
- JPEG, PNG o WebP, con contenido real validado; máximo 5 MiB.
- Archivos en `wwwroot/uploads`, con nombres generados por la aplicación.
- La respuesta devuelve la URL relativa; los archivos se sirven estáticamente.

La carpeta está excluida de Git y del contenido de publicación. En un despliegue, configurar almacenamiento persistente y permisos de escritura para conservar las imágenes entre versiones.

## Pruebas

Con Docker iniciado y acceso a las imágenes de contenedores:

```bash
dotnet test
# Opcional: reporte TRX
dotnet test tests/TPN5.Integration/TPN5.Integration.csproj --logger 'trx;LogFileName=resultado.trx'
```

La suite actual contiene **36 casos: 21 de integración y 15 unitarios**. Cubre autenticación, roles, último Administrador, perfiles, revocación, paginado, borrado lógico, cancelados propios, ingresos, carritos, concurrencia de stock, rollback, importes e imágenes.

Testcontainers inicia SQL Server con imagen fijada por digest. Las pruebas crean bases y carpetas temporales propias, aplican las 14 migraciones y eliminan sus recursos. Reemplazan expresamente la conexión de la API; no requieren claves JWT ni contraseñas SQL personales. Si Docker no está disponible, la integración falla: no se omite silenciosamente.

La ejecución registrada del cambio de cancelados aprobó 35 casos; la del log de errores aprobó 36, sin fallos ni omisiones. Es evidencia de esa versión, no una garantía de cobertura de todas las combinaciones posibles ni un reporte de haber probado individualmente los 74 endpoints.

### Verificación del modelo sin base real

```bash
dotnet run --project tools/Schema/Schema.csproj
```

Comprueba entidades, relaciones, filtros y migraciones sin abrir una conexión SQL. La herramienta también permite generar scripts para revisión; consultar los detalles en la documentación antes de trabajar sobre una base existente.

## Alcance de las vistas planificadas

- **Compartida:** login y navegación según rol.
- **Administrador:** dashboard informativo de cuentas activas/inactivas.
- **Empleado:** solicitudes de compra y carga de mercadería.
- **Cliente:** catálogo con cards, carrito y consulta de pedidos; navegación inferior con carrito central.

La reducción del alcance del frontend no elimina los CRUD administrativos del backend. El diseño mínimo no incluye gestión de usuarios, empleados o clientes en las vistas. No se han implementado aquí esas pantallas ni el registro público/cambio de roles propuesto y luego dejado fuera del alcance.

## Registro de errores

Los errores inesperados conservan la respuesta HTTP 500 genérica y se registran
con ILogger y Serilog en archivos JSON por línea, en `logs/errores-.log` bajo la
raíz de contenido de la API. El evento central incluye excepción, método, ruta,
usuario e identificador de petición; no registra cuerpos ni cabeceras de solicitudes.
Los logs no son una auditoría de todas las operaciones de negocio.

La ruta puede configurarse con `ErrorLog__Path`. Mantenerla fuera de `wwwroot` y
otorgar permisos de escritura al proceso. Hay rotación diaria y por tamaño de
10 MiB, con retención de siete archivos. La carpeta predeterminada se excluye de
Git y de publicación; en despliegue elegir almacenamiento persistente. Si hay
varias instancias, usar una ruta de log diferente para cada una.

## Documentación y forma de trabajo

Consultar [DOCUMENTACION-PROYECTO.md](docs/DOCUMENTACION-PROYECTO.md) para contratos, reglas completas, modelo, historial de migraciones y resultados de cada entrega. Swagger es el punto de exploración de la API en ejecución.

Los cambios se trabajan por bloques explicados y autorizados. No aplicar migraciones a bases desconocidas ni agregar migraciones vacías para cambios sin esquema. Las publicaciones se revisan por rama y PR; el merge a `main` queda a cargo del propietario del proyecto.
