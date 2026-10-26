# Documentación del proyecto de inventario TPN 5

Este archivo reúne el contexto de trabajo, las reglas de negocio, el diseño del
modelo, los contratos implementados, las instrucciones de entorno, las migraciones,
las pruebas y el plan de continuidad de la API. Es la documentación vigente que se
debe leer al retomar el proyecto y actualizar en los próximos bloques.

Última actualización: 26 de octubre de 2026, zona America/Buenos_Aires.
Repositorio: [LuAlzuri/TPN-5-Movil-Correcion](https://github.com/LuAlzuri/TPN-5-Movil-Correcion).
API: `TPN°5-Movil`, ASP.NET Core con .NET 9, EF Core 9 y SQL Server.

## Índice

1. [Estado actual y próximo bloque](#1-estado-actual-y-próximo-bloque)
2. [Reglas de trabajo y autorizaciones](#2-reglas-de-trabajo-y-autorizaciones)
3. [Modelo y relaciones](#3-modelo-y-relaciones)
4. [Permisos acordados](#4-permisos-acordados)
5. [Reglas de stock y movimientos pendientes](#5-reglas-de-stock-y-movimientos-pendientes)
6. [Borrado lógico e históricos](#6-borrado-lógico-e-históricos)
7. [Autenticación y perfiles implementados](#7-autenticación-y-perfiles-implementados)
8. [Categorías implementadas](#8-categorías-implementadas)
9. [Empleados implementados](#9-empleados-implementados)
10. [Unicidad de CUIT y CUIL implementada](#10-unicidad-de-cuit-y-cuil-implementada)
11. [CRUD implementados y contratos pendientes](#11-crud-implementados-y-contratos-pendientes)
12. [Entorno y configuración](#12-entorno-y-configuración)
13. [Migraciones y datos existentes](#13-migraciones-y-datos-existentes)
14. [Pruebas y límites de la validación](#14-pruebas-y-límites-de-la-validación)
15. [Plan de trabajo y alcance por autorizar](#15-plan-de-trabajo-y-alcance-por-autorizar)
16. [Historial de entregas](#16-historial-de-entregas)
17. [Fuentes originales y decisiones que las reemplazan](#17-fuentes-originales-y-decisiones-que-las-reemplazan)
18. [Cómo mantener este archivo](#18-cómo-mantener-este-archivo)

## 1 Estado actual y próximo bloque

La última suite ejecutada aprobó 165 pruebas en SQLite aislada, con 0 fallidas y
0 omitidas. Después de esa ejecución el usuario pidió eliminar todas las pruebas
exitosas: se retiraron la carpeta tests y su proyecto de la solución. Los resultados
se conservan como evidencia histórica, sin una suite versionada actual.
El registro transaccional, la autenticación y los CRUD de
categorías, empleados, clientes y proveedores están implementados. El paso 9.5 de
productos e imágenes está publicado en el PR #10, abierto para revisión y pendiente
de integración por el usuario. Los resultados figuran en la sección 14.
La unicidad de CUIT/CUIL entre clientes y entre proveedores tiene migraciones propias.

Los PR del 1 al 9 están integrados en `main`, según las comprobaciones realizadas
al iniciar el paso 9.5. El último es el PR #9, integrado mediante
`c81dad3`. Verificar Git y GitHub de nuevo en cada sesión: este dato no se actualiza
automáticamente por estar escrito aquí.

La API todavía no está terminada funcionalmente. El próximo bloque es el **paso
10, flujo completo de ingresos y stock**, pendiente de revisión de alcance
y autorización. El flujo de inventario continúa pendiente de sus propios bloques.

| Área | Estado vigente |
| --- | --- |
| Entidades, relaciones y restricciones iniciales | Implementadas; 11 entidades |
| Integración con el modelo nuevo | Implementada |
| JWT y creación consistente de cuentas y perfiles | Implementados |
| Categorías y catálogo de categorías | Paso 9.1 implementado |
| Empleados | Paso 9.2 implementado |
| CUIT/CUIL único en clientes y proveedores | Implementado; dos migraciones nuevas |
| CRUD completo de clientes | Paso 9.3 integrado en PR #8 |
| CRUD completo de proveedores | Paso 9.4 integrado en PR #9 |
| Productos e imágenes | Paso 9.5 publicado en PR #10; pendiente de integración |
| Ingresos y stock | Flujo funcional pendiente |
| Salidas, carrito y stock | Flujo funcional pendiente |
| Auditoría completa e históricos autorizados | Pendientes |
| Base personal del usuario | No se verificó ni migró; no asumir que está preparada |

Los endpoints temporalmente no disponibles devuelven `501` después de los
controles de autorización y validación de entrada que correspondan:

- `POST /api/usuarios`.
- Creación de ingresos y salidas mediante sus servicios actuales.

Los tres tipos de cuenta pueden crearse mediante `POST /api/auth/register`,
exclusivo de Administrador. Los empleados y clientes también pueden crearse
mediante `POST /api/empleados` y `POST /api/clientes`, respectivamente.

## 2 Reglas de trabajo y autorizaciones

Estas reglas provienen del contexto suministrado por el usuario y de las decisiones
de esta conversación. Las indicaciones posteriores del usuario prevalecen sobre
las anteriores, el DER y los documentos históricos.

- Trabajar por bloques. Antes de editar, explicar archivos afectados, comportamiento,
  riesgos y cualquier duda sobre las relaciones entre entidades. Si no hay riesgos
  relevantes, indicar: «Esta acción es segura de realizar».
- Solicitar autorización para el alcance concreto de cada bloque. No avanzar al
  siguiente por tener aprobado el anterior. Una consulta o propuesta no equivale
  a una implementación autorizada.
- Primero reestructurar entidades; después implementar controladores, DTOs y
  servicios por entidad. Crear servicios solo cuando hagan falta.
- Cuando cambie el esquema, generar la migración correspondiente a cada entidad
  agregada o modificada. No crear migraciones vacías.
- No hacer commit, push, publicar ni mergear sin autorización expresa. Las
  autorizaciones de entregas anteriores no son permanentes. El usuario revisa
  e integra los PR; no hacer merge automático.
- No borrar bases, aplicar migraciones a destinos desconocidos ni modificar una
  base personal sin verificar el destino y la autorización.
- No exponer secretos, contraseñas ni tokens reales en Git, logs o respuestas.
- No introducir tablas Rol/UsuarioRol, múltiples roles por cuenta ni RowVersion.
- No usar subagentes ni trabajo delegado sin autorización del usuario.
- Leer `AGENTS.md` si existe y preservar cambios pendientes. No hacer reset,
  sobrescribir trabajo ajeno ni borrar ramas automáticamente.
- Mantener la identidad Git configurada. La autoría histórica fue
  `Lu -- <alzuriluciana@gmail.com>`; en las entregas locales se conservó
  `LucianaAlzuri <alzuriluciana@gmail.com>`. No cambiarla sin autorización.
- El usuario autorizó cambios locales por bloques. Su preferencia inicial por
  cambios manuales no impide las implementaciones locales expresamente aprobadas.

Para retomar: leer este archivo, comprobar `git status`, revisar el estado real de
los PR y las herramientas, y partir de `main` actualizado cuando las dependencias
ya estén integradas. Si un bloque depende de un PR abierto, explicitarlo y usar su
base sin sobrescribir cambios. Nunca asumir que los secretos o instalaciones de
una sesión en la nube existen en el equipo local.

## 3 Modelo y relaciones

El modelo contiene once entidades. Los encabezados y detalles separan los
movimientos; los datos personales y credenciales se centralizan en Usuario.

### 3.1 Usuarios y perfiles

`Usuario`: Id, Nombre, Apellido, CUIT, Email, PasswordHash, Rol y Eliminado.
Cada cuenta tiene exactamente uno de estos roles, con estos valores:
`Administrador`, `Empleado` o `Cliente`.

| Rol de Usuario | Perfil específico | Datos propios del perfil |
| --- | --- | --- |
| Administrador | Ninguno | Ninguno |
| Empleado | Empleado | FechaIngreso |
| Cliente | Cliente | Telefono y Direccion opcionales |

`Empleado.Id` y `Cliente.Id` son a la vez PK y FK hacia `Usuario.Id`: comparten el
identificador de la cuenta. No se permite que una cuenta tenga ambos perfiles.
La cuenta y su perfil se crean en una misma transacción. Las FK no garantizan
por sí solas la correspondencia rol/perfil; el registro la valida y la
autenticación rechaza inconsistencias.

La edición común no cambia el rol ni restaura cuentas. El borrado lógico está en
`Usuario.Eliminado`; no se borra el perfil ni sus referencias históricas.
La columna calculada `Usuario.CUITNormalizado` y su índice filtrado se describen
en la sección de unicidad. No hay unicidad global de CUIL entre todos los roles.

### 3.2 Catálogo

| Entidad | Campos y relaciones principales |
| --- | --- |
| Categoria | Id, Nombre, Descripcion, FechaCreacion, Eliminado; varios Productos |
| Proveedor | Id, Nombre, CUIT, Email, Telefono, Direccion, Eliminado; varios Ingresos; CUITNormalizado calculado |
| Producto | Id, Nombre, Descripcion, Precio, Stock, FechaCreacion, Eliminado, CategoriaId; imagen opcional y detalles de movimientos |
| Imagen | Id, ProductoId, NombreArchivo, RutaRelativa, TipoContenido, TamanoBytes |

Producto no tiene proveedor directo. Su relación con proveedores se obtiene
mediante los ingresos; un producto puede ingresar desde distintos proveedores.
Una categoría puede tener varios productos.

`Imagen.ProductoId` tiene índice único: cada producto admite cero o una imagen.
Los archivos deben almacenarse en `wwwroot/uploads`. `ImagenUrl` se obtiene de
los metadatos de Imagen, actualmente de `RutaRelativa`; no se duplica como
columna persistida de Producto. El paso 9.5 incorpora la carga funcional de archivos.

### 3.3 Movimientos

| Entidad | Campos y relaciones principales |
| --- | --- |
| Ingreso | Id, Fecha, Observaciones, ProveedorId, EmpleadoId creador, Confirmado, FechaConfirmacion, EmpleadoConfirmacionId, Eliminado; varios DetalleIngreso |
| DetalleIngreso | Id, IngresoId, ProductoId, Cantidad, PrecioUnitario, DescAplicado |
| Salida | Id, Fecha, Observaciones, ClienteId, EmpleadoId opcional hasta confirmar, ConfirmarXCliente, ConfirmarXEmpleado, FechaConfirmacionCliente, FechaConfirmacionEmpleado, Eliminado; varios DetalleSalida |
| DetalleSalida | Id, SalidaId, ProductoId, Cantidad, PrecioUnitario, DescAplicado |

`DetalleSalida` tiene un índice único por `(SalidaId, ProductoId)` para impedir
productos duplicados en un carrito. Las restricciones de confirmación relacionan
los estados con sus fechas y empleados. Todas las FK tienen `DeleteBehavior.Restrict`.
No hay RowVersion ni tokens de concurrencia en el modelo.

## 4 Permisos acordados

Esta tabla expresa el diseño funcional autorizado. Los permisos temporales de
controladores todavía pendientes se documentan aparte y no amplían estas reglas.

| Funcionalidad | Administrador | Empleado | Cliente |
| --- | --- | --- | --- |
| CRUD y consultas administrativas de categorías | Sí | No | No |
| Catálogo de categorías activas con Id y Nombre | Sí | Sí | Sí |
| CRUD de empleados | Sí | No | No |
| CRUD de clientes | Sí | No | No |
| CRUD de proveedores | Sí | Sí | No |
| CRUD de productos e imágenes | Sí | Sí | No |
| Ingresos y sus detalles operativos | No | Sí | No |
| Carrito y salidas propias | No | No | Sí |
| Confirmación de salidas solicitadas | Sí | Sí | No |
| Auditorías e históricos autorizados | Sí | No | No |

Administrador no recibe automáticamente todas las funciones operativas. El
empleado usa el catálogo de categorías para el selector de productos; el cliente
puede usarlo en futuros filtros. Ese acceso no permite administrar categorías.
No se acordó acceso anónimo al catálogo.

## 5 Reglas de stock y movimientos pendientes

Las entidades y restricciones existen, pero los flujos de esta sección todavía
deben implementarse y probarse. No interpretar el diseño como funcionalidad lista.

### 5.1 Reglas comunes

- Stock mínimo 0: se permite agotarlo, nunca dejarlo negativo ni vender sin disponibilidad.
- Cantidad de cada detalle: mínimo 1. La cantidad total solicitada de un producto
  no puede superar el stock disponible.
- DescAplicado es un porcentaje de 0 a 100.
- Guardar PrecioUnitario en el detalle para conservar el histórico aunque cambie
  el precio del producto.
- Subtotal previsto: `Cantidad × PrecioUnitario × (1 − DescAplicado / 100)`.
  El total del encabezado es la suma de subtotales. Se requiere redondeo monetario
  uniforme; su criterio concreto debe quedar documentado al implementar el flujo.
- Importes configurados como `decimal(18,2)` y descuentos como `decimal(5,2)`.
- Registrar fecha, proveedor o cliente y empleado según corresponda.
- Las operaciones que alteren stock requieren transacciones explícitas
  `IDbContextTransaction`. Una transacción por sí sola no evita todas las carreras:
  usar actualizaciones condicionadas de estado y stock y comprobar filas afectadas.
- Evitar aplicar o revertir stock más de una vez, sin incorporar RowVersion.

### 5.2 Ingresos

El empleado crea el encabezado sin confirmar. El encabezado no se edita. Sus
detalles se agregan, modifican o eliminan únicamente mientras no esté confirmado.

Confirmar un ingreso con detalles aumenta el stock una sola vez, dentro de una
transacción, registra al confirmador y su fecha y bloquea los detalles. Se conserva
también el empleado creador.

El encabezado admite soft delete. Eliminar uno sin confirmar no afecta el stock.
Eliminar lógicamente uno confirmado revierte las cantidades incorporadas. Si la
reversión deja negativo el stock de cualquier producto, rechazar toda la eliminación.
Confirmación y reversión deben ser atómicas y no ejecutarse dos veces.

### 5.3 Salidas y carrito

El encabezado es el carrito del cliente autenticado. El cliente solo administra
sus propias salidas. Obtener ClienteId y EmpleadoId del JWT validado cuando
corresponda; no aceptarlos desde el JSON para asignar identidad.

Cada producto tiene un solo detalle por salida. Cambiar cantidades actualiza el
detalle existente. No persistir cantidad cero o negativa: quitar el producto
mediante su operación de eliminación.

El carrito no reserva ni descuenta stock. Al confirmar el cliente
(`ConfirmarXCliente = true`), se bloquean sus detalles y la solicitud queda
disponible para confirmación operativa.

El empleado confirma solamente después del cliente: vuelve a comprobar el stock,
registra su ID desde el JWT y la fecha y descuenta todo en una transacción. Si falla
un producto, revierte toda la operación. El diseño también permite que Administrador
confirme salidas, pero este rol no tiene perfil Empleado; antes de implementar ese
caso debe resolverse cómo registrar al confirmador compatible con la FK actual,
sin inventar un EmpleadoId ni alterar los permisos por suposición.

El cliente puede eliminar su salida mientras no esté confirmada por el empleado,
incluso si él ya la solicitó. Después de esa confirmación no puede eliminarla.
No se incluyó cancelación posterior: requeriría un flujo específico de reposición.

## 6 Borrado lógico e históricos

Donde existe Eliminado, el borrado es lógico. Los filtros globales de EF Core
excluyen eliminados de consultas operativas. Los perfiles se filtran según Usuario;
los detalles no tienen Eliminado propio y se filtran según su encabezado. Imagen
se filtra según Producto.

Las validaciones deben impedir nuevas asociaciones a registros eliminados, además
de los filtros. Conservar perfiles, archivos y referencias históricas; no borrar
en cascada. La eliminación actual de productos no elimina físicamente la imagen.

Las consultas de auditoría autorizadas usan `IgnoreQueryFilters()`. Las navegaciones
requeridas o un Include hacia una entidad eliminada pueden ocultar filas completas:
debe atenderse al implementar históricos, no usar una consulta operativa como si
fuera una auditoría completa. Los índices únicos de email, categoría y CUIT/CUIL
no liberan los valores por soft delete, según las reglas de cada uno.

## 7 Autenticación y perfiles implementados

### 7.1 Registro y cuenta

`POST /api/auth/register` requiere Administrador. No hay autorregistro público.
RegisterRequest contiene Nombre, Apellido, CUIT, Email, Password, Rol,
FechaIngreso opcional y Telefono/Direccion opcionales.

Nombre y Apellido admiten hasta 150 caracteres; CUIT hasta 20; Email válido hasta
150. La contraseña tiene entre 12 y 128 caracteres y no puede ser solo espacios.
Se almacena con PasswordHasher. La política se reutiliza en el cambio de contraseña
de la ruta existente de usuarios.

El servicio elimina espacios exteriores de los datos de identidad y guarda el
email en minúsculas. El login busca email sin distinguir mayúsculas. El registro
rechaza emails duplicados incluyendo eliminados; el índice único respalda las
colisiones concurrentes según la collation de SQL Server. El CUIT de clientes
también queda sujeto a la sección 10.

Administrador no admite datos de subperfil. Empleado requiere FechaIngreso, distinta
de `0001-01-01`, y no admite Telefono/Direccion. Cliente no admite FechaIngreso.
La cuenta y su único perfil se guardan dentro de una transacción explícita.

La respuesta es `201`, con Location de la cuenta, sin contraseña ni hash.
`GET /api/auth/me` devuelve UsuarioDto: Id, Nombre, Apellido, CUIT, Email, Rol y
Eliminado. Crear un empleado desde su CRUD reutiliza este registro.

### 7.2 JWT y revocación

`POST /api/auth/login` valida email, contraseña y consistencia del perfil.
Credenciales incorrectas, cuentas eliminadas o inconsistentes devuelven `401`
con mensaje genérico.

Los claims son `sub`, `name`, `email`, `role`, `auth_version` y `jti`.
`sub` es Usuario.Id; `auth_version` es un digest del hash de contraseña, no la
contraseña ni PasswordHash. Permite revocar tokens al cambiar la contraseña
sin agregar columnas. La expiración de la respuesta coincide con `exp` del token.

Se valida algoritmo HS256, firma, emisor, audiencia y vencimiento, con tolerancia
de 30 segundos. `MapInboundClaims = false` y `role` identifica el rol.
No se implementaron refresh tokens. Los tokens anteriores al paso 8 requieren
iniciar sesión otra vez.

Cada petición autenticada consulta la cuenta y comprueba que siga activa, con el
mismo rol, un único perfil válido y la misma versión de credenciales. Agrega una
consulta por petición; si falla la base no se concede acceso por omisión.

Los helpers `Security/UsuarioAutenticado.cs` permiten obtener UsuarioId, ClienteId
y EmpleadoId del principal validado. Usarlos en los futuros flujos. CORS sigue
permisivo para orígenes, métodos y cabeceras, sin credenciales de cookies.

## 8 Categorías implementadas

Paso 9.1: las siguientes rutas requieren Administrador:

| Método | Ruta | Comportamiento |
| --- | --- | --- |
| GET | `/api/categorias` | Listado administrativo paginado y búsqueda |
| GET | `/api/categorias/{id}` | Consulta individual |
| POST | `/api/categorias` | Crear; 201 con Location |
| PUT | `/api/categorias/{id}` | Editar; 204 |
| DELETE | `/api/categorias/{id}` | Soft delete; 204 |

`GET /api/categorias/catalogo` admite Administrador, Empleado y Cliente autenticados.
Devuelve solo `id` y `nombre` de categorías activas, ordenadas por Nombre e Id.
Sirve al selector de productos y a futuros filtros de clientes.

Nombre es obligatorio, no admite solo espacios y tiene máximo 100 caracteres.
Descripcion es opcional, máximo 1000. Se eliminan espacios exteriores de ambos;
descripción vacía se guarda como null. CategoriaResponse contiene Id, Nombre,
Descripcion y FechaCreacion.

Los nombres deben ser únicos incluso entre eliminados. Duplicados devuelven 409;
se compara usando la misma columna y collation que el índice, sin imponer otra
regla de mayúsculas o acentos en memoria. Los conflictos del índice SQL Server
también se traducen a 409.

El listado usa `page` positivo, `pageSize` de 1 a 100 (predeterminados 1 y 10),
orden Nombre e Id y `busqueda` recortada sobre Nombre. Rechaza con 400 parámetros
inválidos o desplazamientos fuera de Int32; páginas fuera del resultado devuelven
Items vacío. PaginatedResult incluye Page, PageSize, TotalCount, TotalPages,
HasNext, HasPrevious e Items.

Eliminar conserva productos y FK. La categoría deja de estar disponible para
lectura operativa, edición o eliminación posterior (404). Crear o editar un
producto ya exige categoría activa. No se agregaron auditorías ni cambios de
esquema en este paso.

## 9 Empleados implementados

Paso 9.2: todas las rutas `/api/empleados` requieren Administrador.

| Método | Ruta | Comportamiento |
| --- | --- | --- |
| GET | `/api/empleados` | Activos con búsqueda y paginado |
| GET | `/api/empleados/{id}` | Consulta individual |
| POST | `/api/empleados` | Registro transaccional de Usuario y Empleado; 201 y Location |
| PUT | `/api/empleados/{id}` | Edición conjunta de datos personales y FechaIngreso; 204 |
| DELETE | `/api/empleados/{id}` | Marca Usuario.Eliminado; 204 |

EmpleadoResponse contiene Id, Nombre, Apellido, CUIT, Email y FechaIngreso.
Las rutas solo consultan/modifican perfiles activos con rol Empleado y sin perfil
Cliente. Un ID inexistente, eliminado o de otro tipo de cuenta devuelve 404.
No se incorporaron restauración ni auditoría.

La creación requiere Nombre/Apellido (máximo 150), CUIT (máximo 20), Email válido
(máximo 150), Password (12 a 128) y FechaIngreso. La edición requiere esos campos
salvo Password. FechaIngreso no puede faltar ni ser `0001-01-01`; no se impuso que
deba ser pasada porque no se acordó esa restricción.

Se guardan textos sin espacios exteriores y email en minúsculas. Se rechazan emails
duplicados incluyendo otros roles y cuentas eliminadas, sin distinguir mayúsculas;
los conflictos SQL Server de claves únicas al editar se traducen a 409. El rol se
fija en el servidor. Los DTOs rechazan propiedades JSON desconocidas, incluyendo
Rol, Id o datos de Cliente; editar rechaza Password. La contraseña se modifica
mediante la ruta existente de usuarios. No se exige CUIT único entre empleados.

La creación usa la transacción explícita de AuthService.RegisterAsync. La edición
de Usuario y Empleado es atómica mediante la transacción de SaveChanges. El soft
delete conserva perfil y movimientos e invalida el acceso en la siguiente petición
mediante la comprobación de autenticación existente.

El listado usa los límites de paginado de categorías y orden Apellido, Nombre e Id.
`busqueda` recortada consulta nombre completo, email o CUIT. No cambió el esquema.

## 10 Unicidad de CUIT y CUIL implementada

Cada identificador puede pertenecer a **un solo Cliente** y a **un solo Proveedor**.
Los grupos son independientes: se permite un Cliente y un Proveedor con el mismo
identificador. La regla incluye eliminados. No es unicidad global de Usuario y
no restringe el CUIT de Empleados ni Administradores.

Se mantiene `CUIT` como nombre del campo y del contrato JSON. Para comparar se
ignoran guiones, espacios ASCII, tabulaciones y saltos CR/LF. Por ejemplo,
`20-12345678-9`, `20123456789` y `20 12345678 9` son equivalentes. No se incorporó
validación de dígito verificador ni longitud exacta de 11 dígitos.

`Services/CuitUnico.cs` centraliza las consultas y normalización. Se rechazan con
400 entradas vacías, de más de 20 caracteres o vacías después de quitar separadores.
Los duplicados detectados devuelven 409, excluyendo el propio ID al editar.

Se cubren estas rutas:

- Registro Cliente en `/api/auth/register`.
- Edición Cliente en `/api/clientes/{id}`.
- Edición de un Usuario con rol Cliente en `/api/usuarios/{id}`.
- Creación y edición de `/api/proveedores`.

Los conflictos SQL Server 2601/2627 se traducen a 409 si una carrera supera la
consulta previa. En la base, Usuario y Proveedor tienen una columna calculada
persistida `CUITNormalizado`. Se conserva el valor original y no se expone esa
columna en los DTOs. Las escrituras guardan el formato ingresado sin espacios
exteriores; la comparación se realiza sobre la clave calculada.

Usuario tiene índice único en CUITNormalizado filtrado por `Rol = 'Cliente'`, sin
excluir Eliminado. Proveedor tiene índice único sin filtro en CUITNormalizado y
conserva el índice único original de CUIT. Los índices protegen escrituras directas
y concurrentes, además de los controladores.

Hay dos migraciones reales, una por entidad modificada, detalladas en la sección 13.
El ajuste de unicidad fue independiente del CRUD completo de clientes, que se
implementó después en el paso 9.3.

## 11 CRUD implementados y contratos pendientes

Clientes está implementado en el paso 9.3, proveedores en el 9.4 y productos e
imágenes en el 9.5. Los apartados
pendientes describen código de transición, no una validación integral de esos CRUD.
Los límites estrictos de paginado de categorías, empleados, clientes, proveedores y productos no
deben darse por implementados en todas las rutas: el helper general todavía
normaliza page a un mínimo de 1 y pageSize al intervalo 1 a 100.

### 11.1 Clientes implementados

Todas las rutas de ClientesController requieren Administrador, incluido el
catálogo. El Cliente autenticado no puede administrar estas rutas ni usar el
catálogo para consultar datos personales de otros clientes.

| Método | Ruta | Comportamiento |
| --- | --- | --- |
| GET | `/api/clientes` | Clientes activos con búsqueda y paginado |
| GET | `/api/clientes/catalogo` | Catálogo administrativo de clientes activos |
| GET | `/api/clientes/{id}` | Consulta individual |
| POST | `/api/clientes` | Cuenta y perfil Cliente en una transacción; 201 y Location |
| PUT | `/api/clientes/{id}` | Datos personales, teléfono y dirección; 204 |
| DELETE | `/api/clientes/{id}` | Usuario.Eliminado; 204 |

ClienteResponse contiene Id, Nombre, Apellido, CUIT, Email, Telefono y Direccion;
no incluye contraseña ni hash. Las rutas operan únicamente sobre perfiles activos
con rol Cliente y sin perfil Empleado. IDs inexistentes, eliminados, de otro tipo
de cuenta o con perfiles inconsistentes devuelven 404. No se agregaron auditorías
ni restauración de cuentas.

ClienteCreateRequest requiere Nombre/Apellido (máximo 150), CUIT (máximo 20),
Email válido (máximo 150) y Password (12 a 128 caracteres, no solo espacios).
Telefono y Direccion son opcionales, con máximos de 50 y 300. La creación elimina
espacios exteriores de textos, guarda email en minúsculas y convierte opcionales
vacíos o de solo espacios a null. El rol Cliente se fija en el servidor y
AuthService.RegisterAsync crea Usuario y su perfil de PK compartida en una
transacción explícita. Si falla el perfil, no queda una cuenta parcial.

ClienteUpdateRequest tiene los mismos datos salvo Password. Es una edición completa:
los opcionales omitidos se guardan como null. Los cambios en Usuario y Cliente se
guardan atómicamente con la transacción de SaveChanges; un fallo del perfil revierte
también los datos personales. No se modifica el hash de contraseña ni el rol.
Los DTOs rechazan propiedades JSON desconocidas como Rol, Id, FechaIngreso y,
en edición, Password. La contraseña se sigue modificando por la ruta de usuarios.

Se reutiliza la unicidad de CUIL de la sección 10, incluyendo formatos equivalentes
y clientes eliminados. Email no se repite entre cuentas de ningún rol, incluyendo
eliminadas; la validación no distingue mayúsculas y excluye el propio ID. Los
duplicados devuelven 409 y los conflictos de índices SQL Server también se traducen
a 409. Se conservan la clave calculada y las migraciones existentes, sin cambiar
el esquema en este bloque.

El listado admite page positivo y pageSize de 1 a 100 (1 y 10 por defecto).
Rechaza con 400 los valores inválidos y desplazamientos fuera de Int32; una página
fuera del resultado devuelve Items vacío. Listado y catálogo ordenan por Apellido,
Nombre e Id. La búsqueda recortada consulta nombre completo, CUIT y email mediante
coincidencia parcial; no es una consulta exacta ni normaliza guiones para buscar.

El borrado marca Usuario.Eliminado y conserva perfil, salidas y demás relaciones.
El cliente desaparece de listado, catálogo y detalle y pierde acceso con tokens
anteriores en la siguiente petición. Tampoco puede iniciar sesión nuevamente.
Se mantienen reservados email y CUIL; repetir edición o borrado devuelve 404.

### 11.2 Usuarios pendientes de revisión integral

UsuariosController es exclusivo de Administrador: GET paginado, GET por ID,
PUT y DELETE operan; POST sigue en 501. UsuarioDto usa Nombre y Apellido separados,
CUIT y Eliminado. La edición no cambia Rol, no restaura cuentas ni borra perfiles;
puede cambiar Password, sujeta a la política del paso 8. Email y otras validaciones
de esa ruta requieren revisión; no asumir toda la normalización del registro.

### 11.3 Proveedores implementados

Todas las rutas requieren Administrador o Empleado, incluido el catálogo.
Cliente recibe 403 y los anónimos 401. Se reemplaza el permiso temporal de lectura
para cualquier cuenta autenticada por los permisos acordados para este bloque.

| Método | Ruta | Comportamiento |
| --- | --- | --- |
| GET | `/api/proveedores` | Proveedores activos con búsqueda y paginado |
| GET | `/api/proveedores/catalogo` | Catálogo operativo completo de proveedores activos |
| GET | `/api/proveedores/{id}` | Consulta individual |
| POST | `/api/proveedores` | Crear; 201 y Location de la consulta individual |
| PUT | `/api/proveedores/{id}` | Edición completa; 204 |
| DELETE | `/api/proveedores/{id}` | Marca Proveedor.Eliminado; 204 |

ProveedorResponse conserva Id, Nombre, CUIT, Email, Telefono y Direccion, también
en catálogo. ProveedorRequest se usa para crear y editar: Nombre obligatorio con
máximo 150, CUIT obligatorio con máximo 20, Email opcional válido hasta 150,
Telefono opcional hasta 50 y Direccion opcional hasta 300. Rechaza propiedades
JSON desconocidas como Id o Eliminado; no permite restaurar por edición.

Nombre y CUIT se guardan sin espacios exteriores. El email se recorta, convierte
a minúsculas y, si queda vacío, a null antes de validarlo como opcional. Teléfono
y dirección también se recortan; vacíos o solo espacios se guardan como null.
Omitir opcionales en PUT los deja en null. El email de proveedores no es único:
la regla de email único corresponde a cuentas de Usuario.

Se reutiliza CuitUnico y los índices existentes para impedir CUIT/CUIL equivalentes
entre proveedores activos o eliminados. El propio ID se excluye al editar y los
conflictos se devuelven como 409, incluyendo colisiones de índices SQL Server.
La comparación de equivalencia conserva las reglas de la sección 10; no se
introduce validación de dígito verificador ni longitud exacta de 11 dígitos.

Listado y catálogo ordenan por Nombre e Id. El listado acepta page positivo y
pageSize de 1 a 100 (1 y 10 por defecto); parámetros inválidos o desplazamientos
fuera de Int32 devuelven 400. Una página fuera del resultado devuelve Items vacío.
La búsqueda recortada consulta Nombre, CUIT y Email por coincidencia parcial,
admite proveedores sin email y no normaliza guiones al buscar.

Eliminar conserva ingresos, sus detalles y stock, incluso cuando el proveedor tiene
ingresos confirmados. El proveedor desaparece de listado, catálogo y consulta por
ID; su edición o eliminación posterior devuelve 404. El catálogo y las consultas
operativas ya no lo ofrecen para nuevas selecciones. La validación de proveedor
activo al guardar un ingreso corresponde al paso 10: la creación de ingresos
continúa temporalmente en 501 y no se implementa en este bloque.

No se agregan endpoints de auditoría ni se cambia el modelo o el esquema. Los
históricos completos siguen pendientes de sus propias consultas autorizadas.

### 11.4 Productos e imágenes del paso 9.5

| Método | Ruta | Permisos y comportamiento |
| --- | --- | --- |
| GET | `/api/productos` | Administrador, Empleado y Cliente autenticados; listado paginado |
| GET | `/api/productos/{id}` | Los mismos tres roles; producto activo con categoría e ImagenUrl |
| GET | `/api/productos/{id}/movimientos` | Administrador y Empleado; movimientos confirmados |
| POST | `/api/productos` | Administrador y Empleado; 201 y Location; stock inicial 0 |
| PUT | `/api/productos/{id}` | Administrador y Empleado; edición de catálogo; 204 |
| DELETE | `/api/productos/{id}` | Administrador y Empleado; soft delete; 204 |
| POST | `/api/productos/{id}/imagen` | Administrador y Empleado; multipart con campo file; 200 con metadatos |

Los DTOs requieren Nombre hasta 150, Precio y CategoriaId positivo. Descripcion
es opcional hasta 1000. Precio debe estar entre 0 y 9999999999999999.99 con hasta
dos decimales: entradas de mayor precisión no se redondean silenciosamente.
Omitir precio o categoría devuelve 400. Se rechazan campos desconocidos como
Stock, ProveedorId, ImagenUrl o Eliminado. Nombre y descripción se recortan;
descripción vacía se guarda como null.

Crear requiere categoría activa; reasignar requiere otra categoría activa.
Conservar una categoría histórica eliminada está permitido al editar, a diferencia
del comportamiento anterior que rechazaba toda edición en ese caso. El producto
existente sigue visible con su categoría e imagen después de eliminar la categoría.
Las consultas omiten filtros globales para conservar esas referencias y mantienen
explícitamente `!Producto.Eliminado`.

El listado usa page positivo, pageSize de 1 a 100 (1 y 10 por defecto), busqueda,
categoriaId y proveedorId. Los filtros de ID deben ser positivos; IDs positivos
sin coincidencias y páginas fuera del resultado devuelven Items vacío. Ordena
por Nombre e Id. La búsqueda recortada consulta nombre o descripción, incluida
descripción null. El filtro proveedor recorre detalles de ingresos no eliminados;
no crea proveedor directo ni exige confirmación para ese filtro.

Los movimientos combinan ingresos confirmados y salidas confirmadas por empleado,
excluyendo encabezados eliminados explícitamente. Conservan contrapartes históricas
aunque proveedor, cliente o empleados estén eliminados. Ordenan por Fecha
descendente, Tipo, Id de encabezado e Id de detalle. Las salidas usan cantidades
negativas y el precio unitario histórico. Se valida el paginado. No habilitan
auditoría completa ni movimientos de productos eliminados, que siguen en 404.

El CRUD y la carga de imágenes no modifican el stock salvo inicializarlo en 0 al
crear. El soft delete conserva relaciones, metadatos y archivo de imagen. El
producto eliminado queda fuera de listado, detalle, edición y nuevas cargas.

#### Validación y almacenamiento de imágenes

Se admiten JPG/JPEG, PNG y WebP hasta 5 MiB de archivo inclusive. Deben coincidir
extensión, MIME (image/jpeg, image/png o image/webp) y formato real. SkiaSharp
decodifica píxeles para rechazar contenido falso o incompleto. Se aceptan imágenes
estáticas de hasta 8192 píxeles por lado y 20 megapíxeles para acotar memoria.
Archivos vacíos, formatos inválidos, contenido dañado o dimensiones no admitidas
devuelven 400; archivo mayor a 5 MiB devuelve 413. La petición tiene 64 KiB
adicionales para multipart. Una petición sin file devuelve 400.

El archivo validado se conserva sin recodificar, incluidos sus metadatos originales.
Se guarda bajo wwwroot/uploads con nombre GUID generado y extensión del formato
(.jpeg se guarda como .jpg), sin usar la ruta aportada por el cliente.
ImagenResponse contiene Id, ProductoId, NombreArchivo, ImagenUrl, TipoContenido
y TamanoBytes. La URL de producto deriva de Imagen.RutaRelativa, sin columna nueva.

Se conserva el comportamiento de archivos estáticos de la aplicación: la URL
`/uploads/<nombre>` es pública, incluso si luego se elimina lógicamente el producto.
Listado y detalle de catálogo requieren autenticación, pero conocer la URL permite
descargar el archivo. Se agrega `X-Content-Type-Options: nosniff`.

ImagenProductoService valida antes de escribir. IAlmacenImagenes guarda el archivo
nuevo con FileMode.CreateNew. Una transacción Serializable hace una actualización
condicionada sin cambio de Nombre sobre el producto activo para mantener su bloqueo
hasta confirmar y serializar cargas/borrado sin RowVersion. Se inserta o actualiza
la única Imagen por producto, manteniendo su Id al reemplazar. Solo después del
commit se intenta limpiar el archivo anterior.

Un fallo antes de intentar commit revierte la base y limpia el nuevo archivo;
el anterior permanece intacto. Un fallo de escritura limpia su parcial. Base y
filesystem no comparten una transacción distribuida: si se pierde la confirmación
de commit se conservan ambos archivos y se registra una advertencia para revisión,
evitando borrar uno posiblemente referenciado. Si falla la limpieza posterior,
la imagen nueva sigue válida y puede quedar un huérfano para reconciliar. La
limpieza solo admite nombres generados; no borra rutas arbitrarias ni archivos
históricos de wwwroot/upload.

wwwroot/uploads queda excluido de Git y del contenido de publicación. Al desplegar,
conservar y respaldar ese almacenamiento persistente junto con la base, sin
reemplazarlo por una carpeta vacía. La carpeta original wwwroot/upload se conserva
sin migración automática. La validación usó web roots temporales aislados y los
limpió al terminar. Sus fuentes se retiraron luego por instrucción del usuario.

Se agregan [SkiaSharp 4.153.1](https://www.nuget.org/packages/SkiaSharp/4.153.1)
y Linux.NoDependencies de igual versión, condicionado a host Linux o RID linux-x64/
linux-arm64. El paquete principal aporta runtimes Windows/macOS. Validar el RID
y las bibliotecas nativas al desplegar; selección de paquetes según la
[documentación oficial](https://github.com/mono/SkiaSharp/blob/main/documentation/dev/packages.md).
No se cambian las once entidades, sus relaciones ni las trece migraciones.

### 11.5 Ingresos y salidas

Las consultas actuales de ambos controladores son exclusivas de Administrador y
Empleado. Los listados admiten productoId y desde/hasta, más proveedorId para
ingresos o clienteId para salidas. Ordenan por Fecha e Id descendentes y paginan.

IngresoResponse y SalidaResponse exponen encabezado, estados, fechas y detalles.
IngresoRequest recibe ProveedorId y Observaciones. SalidaRequest solo Observaciones:
no se reciben ClienteId ni EmpleadoId para elegir la identidad operativa.

MovimientoService mantiene bloqueada la creación con 501. No se conserva la
implementación anterior que modificaba stock al crear el movimiento. Las consultas
de salidas propias del Cliente esperan la implementación del alcance por identidad.
Las consultas temporales de Administrador no lo autorizan a crear ingresos.

## 12 Entorno y configuración

### 12.1 Herramientas

El proyecto tiene target .NET 9 y paquetes EF Core 9.0.11. Se requiere SDK compatible
y `dotnet-ef 9.0.11` para generar migraciones. El archivo histórico
`TPN°5-Movil/dotnet-tools.json` indica EF 10: no restaurarlo a ciegas. El entorno
local tenía dotnet-ef global 9.0.5 y varios SDK; se utilizó una instalación auxiliar
de dotnet-ef 9.0.11 sin cambiar la herramienta global. Las compilaciones locales
se ejecutaron con el SDK disponible 10.0.401 sobre el target net9.0.

No depender de la ruta auxiliar de una sesión anterior: verificar herramientas
antes de ejecutar los comandos `dotnet ef` de este archivo. La generación de
migraciones y SQL no necesita una conexión real usando tools/Schema.

### 12.2 Configuración de la API

`appsettings.example.json` es una plantilla sin secretos. Definir de forma segura:
`ConnectionStrings__DefaultConnection`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__Key`
y, opcionalmente, `Jwt__ExpireMinutes`.

`Security/JwtSettings.cs` valida la configuración al arrancar. La clave debe tener
al menos 32 bytes y no existe una predeterminada. ExpireMinutes acepta 1 a 1440,
con 120 por defecto. Usar configuración local ignorada, secretos de desarrollo
o variables de entorno; no copiar conexiones ni claves reales a Git.

### 12.3 Primer Administrador

El esquema debe estar preparado previamente en un destino conocido y autorizado.
El comando explícito no aplica migraciones y no inicia HTTP:

```powershell
dotnet run --project 'TPN°5-Movil/TPN°5-Movil.csproj' --no-launch-profile -- --crear-administrador
```

Requiere `Bootstrap__Nombre`, `Bootstrap__Apellido`, `Bootstrap__CUIT`,
`Bootstrap__Email` y `Bootstrap__Password` configurados de forma segura. No hay
contraseña predeterminada. No escribirla en el historial ni imprimirla. Si ya hay
un Administrador, incluso eliminado, se rechaza otra creación. Usa una transacción
Serializable. La configuración JWT sigue siendo necesaria para construir la aplicación.

Ejemplo histórico de entrada protegida de contraseña en Git Bash, con el resto de
variables ya configuradas, para un destino autorizado:

```bash
read -r -s -p 'Contraseña inicial: ' Bootstrap__Password
export Bootstrap__Password
dotnet run --project 'TPN°5-Movil/TPN°5-Movil.csproj' --no-launch-profile -- --crear-administrador
unset Bootstrap__Password
```

### 12.4 Inicialización y prueba manual HTTP

DbInitializer no se ejecuta al iniciar la API y no aplica migraciones. Su catálogo
de ejemplo tiene productos con stock 0, sin proveedor directo ni cuentas con
contraseña fija. No ejecutarlo sin preparar y autorizar el esquema/destino.

Para iniciar la API local una vez configurada y preparada la base, desde la raíz:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5087'
dotnet run --project 'TPN°5-Movil/TPN°5-Movil.csproj' --no-launch-profile
```

Swagger describe los contratos actuales. En Postman se obtiene un JWT mediante
login y se carga como Bearer Token en Authorization para las rutas protegidas,
o se envía `Authorization: Bearer <token>` sin publicar el valor. La URL de un
despliegue histórico del PDF no se considera un destino actual verificado.

## 13 Migraciones y datos existentes

### 13.1 Historial y restricciones de aplicación

Las once migraciones iniciales se diseñaron para una **base nueva y vacía**,
una por entidad y en orden de dependencia. Las migraciones Inicial antiguas
se conservaron como texto en `docs/migrations-legacy/*.cs.txt`, fuera de compilación.
La cadena nueva no convierte datos de esa base anterior. No aplicarla sobre el
historial antiguo, aunque el script sea idempotente. No borrar ni reiniciar bases
para resolver diferencias sin autorización.

Actualmente hay 13 migraciones: las once iniciales y estas dos del ajuste de CUIL:

| Migración | Cambio real |
| --- | --- |
| `20261026210457_CuilUnicoClientes` | CUITNormalizado calculado en Usuarios e índice único filtrado por rol Cliente |
| `20261026210619_CuilUnicoProveedores` | CUITNormalizado calculado en Proveedores e índice único sin filtro |

No hubo cambios de esquema en los pasos 7, 8, 9.1, 9.2, 9.3, 9.4 ni 9.5; no se crearon migraciones
vacías. El snapshot vigente está en `TPN°5-Movil/Migrations`, para que lo descubra
tanto la API como el proyecto auxiliar.

### 13.2 Inspección del modelo sin conexión

Desde la raíz, con dotnet-ef 9.0.11 verificado:

```powershell
dotnet run --project tools/Schema/Schema.csproj
dotnet ef migrations has-pending-model-changes --project tools/Schema/Schema.csproj
dotnet ef migrations script --project tools/Schema/Schema.csproj --idempotent --output esquema.sql
```

tools/Schema enlaza las entidades, contexto y migraciones reales; no mantiene
copias. Su factory de diseño no abre una conexión para generar SQL. La conexión
de respaldo con nombre `TPN5_DesignOnly` no debe usarse como destino real.
Los scripts generados para revisión son intermediarios, no documentación paralela.

EF puede generar el snapshot en una carpeta del proyecto auxiliar por las rutas
enlazadas: conservar un único snapshot en la carpeta de migraciones de la API y
comprobar que no haya dos clases AppDbContextModelSnapshot compiladas.

### 13.3 Duplicados antes de las migraciones de CUIL

Las columnas nuevas son necesarias antes de usar la versión del ajuste de CUIL
contra una base existente. No se aplicaron a la base del usuario. Si existen
identificadores equivalentes repetidos, fallará la creación del índice único.
No hay eliminación, combinación ni corrección automática de registros.

Esta consulta de solo lectura permite detectar los grupos repetidos, incluyendo
eliminados, antes de aplicar las migraciones a un destino autorizado:

```sql
WITH Identificadores AS (
    SELECT 'Cliente' AS Tipo, CUIT
    FROM Usuarios
    WHERE Rol = 'Cliente'
    UNION ALL
    SELECT 'Proveedor', CUIT
    FROM Proveedores
), Normalizados AS (
    SELECT Tipo,
        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(CUIT, '-', ''), ' ', ''),
            CHAR(9), ''), CHAR(10), ''), CHAR(13), '') AS CUITNormalizado
    FROM Identificadores
)
SELECT Tipo, CUITNormalizado, COUNT(*) AS Cantidad
FROM Normalizados
GROUP BY Tipo, CUITNormalizado
HAVING COUNT(*) > 1;
```

Cada duplicado existente requiere una decisión explícita; no resolverlo borrando
filas ni modificando identidades por suposición.

### 13.4 Aplicación controlada

Solo tras verificar conexión, historial, duplicados y autorización, el comando
de aplicación desde la raíz es:

```powershell
dotnet ef database update --project 'TPN°5-Movil/TPN°5-Movil.csproj'
```

Este ejemplo no autoriza a ejecutarlo. La API requiere configuración JWT válida
si se usa su proyecto como startup. Preferir primero la inspección sin conexión.
El despliegue debe coordinar esquema y código; integrar un PR no migra la base.

## 14 Pruebas y límites de la validación

### 14.1 Verificación vigente y retiro de la suite

Desde la raíz:

```powershell
dotnet build 'TPN°5-Movil.slnx'
dotnet run --project tools/Schema/Schema.csproj
```

Tras aprobar las 165 pruebas del paso 9.5, el usuario pidió eliminar todas las
pruebas exitosas. Se quitaron tests/Auth.Integration y su referencia en la solución,
incluidas las fuentes de autenticación, categorías, empleados, clientes, CUIL,
proveedores, productos e imágenes. No queda proyecto de pruebas versionado.
El comando `dotnet test tests/Auth.Integration/Auth.Integration.csproj` deja de
ser un comando vigente. La instrucción de retiro prevalece sobre las referencias
históricas anteriores. tools/Schema se conserva como herramienta de migraciones
e inspección del modelo; no es el proyecto de pruebas retirado.

La suite retirada usaba SQLite en memoria, aislada por caso, sin credenciales
reales. WebApplicationFactory ejercitaba HTTP, servicios y transacciones; los
archivos de imágenes iban a carpetas temporales independientes. Con la variable
AUTH_TEST_SQLSERVER_CONNECTION podía crear bases TPN5_AuthTest_<GUID> y aplicar
migraciones en una instancia descartable. Esta descripción es histórica: el
paso 9.5 se ejecutó en SQLite, sin usar ni modificar una base personal.

`tools/smoke_step7.py` es histórico: firmaba tokens artificiales sin cuenta real
que ya no sirven para rutas autenticadas. No sustituye la validación retirada
ni debe interpretarse como una suite vigente.

### 14.2 Evidencia por entrega

| Entrega | Resultado registrado | Límite |
| --- | --- | --- |
| Paso 1 | Modelo y SQL generados; 11 migraciones | En ese cierre todavía no se había recuperado la compilación ni ejecutado SQL Server |
| Paso 7 | Compilación sin errores/advertencias; 12 comprobaciones del modelo y 15 HTTP, Swagger y CORS | Tokens artificiales; sin migraciones, seed, stock ni concurrencia reales |
| Paso 8 | 25 pruebas en SQLite y las mismas 25 en SQL Server 2022, sin omisiones | Pruebas en instancia descartable; no base del usuario |
| Paso 9.1 | 34 pruebas, 0 fallidas y 0 omitidas | Funcionales en SQLite; comparación del modelo SQL Server sin conexión |
| Paso 9.2 | 54 pruebas, 0 fallidas y 0 omitidas; compilación sin advertencias | Funcionales en SQLite; sin aplicación a bases personales |
| Unicidad de CUIL | 72 pruebas, 0 fallidas y 0 omitidas; verificación de 13 migraciones | Funcionales en SQLite; SQL generado/revisado, sin ejecutar las dos migraciones nuevas en SQL Server real |
| Paso 9.3 | 99 pruebas, 0 fallidas y 0 omitidas; compilación sin advertencias | Funcionales en SQLite; sin pruebas funcionales en SQL Server ni migraciones nuevas |
| Paso 9.4 | 119 pruebas, 0 fallidas y 0 omitidas; compilación sin advertencias | Funcionales en SQLite; sin pruebas funcionales en SQL Server ni migraciones nuevas |
| Paso 9.5 | 165 pruebas, 0 fallidas y 0 omitidas; compilación sin advertencias | Funcionales en SQLite y filesystem temporal en Windows; sin pruebas funcionales en SQL Server/Linux; suite retirada después por instrucción del usuario |

Los números son totales de la suite en cada entrega, no cantidades acumulables.
Durante el paso 8 se aplicaron las once migraciones iniciales a bases nuevas de
prueba y se probó el CLI del primer Administrador, incluido el rechazo de la
segunda creación. La instancia descartable se detuvo al terminar.

La última suite, retirada tras aprobar, cubrió autenticación, perfiles, permisos, revocación, CORS,
categorías, empleados, clientes, proveedores, rollback en creación/edición, paginado, conservación de
históricos y CUIT/CUIL único, incluyendo formatos equivalentes, eliminados,
rutas alternativas y rechazo directo por índices. Confirmó que el modelo SQL
Server coincide con el snapshot sin abrir una conexión.

tools/Schema comprueba once entidades, PK compartidas, ausencia de RowVersion,
una imagen por producto, producto único por carrito, empleado opcional antes de
confirmar, FK Restrict, filtros, índices de CUIL y trece migraciones descubiertas.

La entrega de consolidación documental no cambió código ejecutable ni repitió las
pruebas funcionales. El paso 9.3 sí cambia código y ejecuta nuevamente la suite;
sus resultados se registran por separado.

Las 27 pruebas incorporadas en el paso 9.3 verifican acceso exclusivo de
Administrador en todas las rutas de clientes, creación de cuenta/perfil y Location,
normalización, opcionales y límites, contraseña solo en creación, rechazo de
propiedades inesperadas, duplicados activos/eliminados, búsqueda, orden estable,
paginado, perfiles inconsistentes, cuentas de otro rol, preservación de salidas,
revocación y rollback de creación/edición. Las 72 pruebas previas también pasaron.

El paso 9.4 incorpora 20 casos de proveedores y vuelve a aprobar los 99 previos.
Verifica CRUD completo para Administrador y Empleado; bloqueo de anónimos y
Cliente en todas las rutas; límites y datos inválidos; opcionales vacíos, omitidos
y en longitudes máximas; rechazo de campos desconocidos; búsqueda con email null;
orden estable y paginado; duplicados activos y eliminados; y preservación de
ingresos confirmados, detalles y stock tras soft delete. No se aplicaron migraciones
ni se modificaron bases personales. La comparación del modelo SQL Server con el
snapshot estuvo incluida en esa suite, sin abrir conexión, antes de su retiro.

El paso 9.5 agregó 46 casos a los 119 anteriores. Verificó permisos, validación y
límites de precio, categorías activas al crear/reasignar, conservación de categoría
histórica, paginado, filtros y contrapartes eliminadas. Las pruebas de imágenes
validaron JPG/JPEG/PNG/WebP reales, archivos inválidos, tamaño, dimensiones,
metadatos, descarga estática, reemplazo sin duplicar filas, conservación al soft
delete, fallos de disco/base, cancelación de escritura, borrado del producto
durante la carga y confirmación de commit perdida. Todas aprobaron antes del retiro.
No hubo cambio de esquema ni migraciones nuevas. No se afirma que estas pruebas
sean ejecutables desde el checkout posterior a su eliminación.

Después del retiro se compiló la solución sin errores ni advertencias y se
ejecutó tools/Schema: confirmó once entidades, trece migraciones y las restricciones
del modelo, sin conectar a una base. El retiro no deja referencias rotas al
proyecto de pruebas en la solución.

## 15 Plan de trabajo y alcance por autorizar

El plan original se mantiene con los siguientes estados:

| Paso | Trabajo | Estado |
| --- | --- | --- |
| 1 | Diseño definitivo de entidades y estrategia de migraciones | Implementado |
| 2 | Usuarios, empleados y clientes como entidades | Incluido en la implementación inicial del modelo |
| 3 | Categorías, proveedores y productos como entidades | Incluido en el modelo |
| 4 | Encabezados y detalles de ingreso como entidades | Incluido en el modelo |
| 5 | Encabezados y detalles de salida como entidades | Incluido en el modelo |
| 6 | Entidad Imagen | Incluida en el modelo |
| 7 | Integración y recuperación de compilación | Implementado |
| 8 | JWT y creación consistente de cuentas/perfiles | Implementado |
| 9.1 | CRUD de categorías | Implementado |
| 9.2 | CRUD de empleados | Implementado |
| Ajuste adicional | CUIT/CUIL único en clientes y proveedores | Implementado |
| 9.3 | CRUD de clientes | Implementado e integrado en PR #8 |
| 9.4 | CRUD de proveedores | Implementado e integrado en PR #9 |
| 9.5 | CRUD de productos e imágenes | Implementado y publicado en PR #10; pendiente de integración |
| 10 | Flujo completo de ingresos y stock | Próximo bloque; alcance por autorizar |
| 11 | Flujo completo de salidas y stock | Pendiente |
| 12 | Validación integral y entrega | Pendiente |

Los pasos 2 al 6 se revisaron después sin duplicar entidades ni migraciones. Que
el modelo exista no significa que sus flujos funcionales estén terminados.

El usuario autorizó específicamente el alcance del paso 9.3 descrito en la sección
11.1. Se implementa en ClientesController, ClienteDtos y ClienteTests, actualizando
este mismo documento. No se modifican AuthService, las entidades ni el CRUD general
de usuarios en este bloque. Posteriormente autorizó el commit y la subida de esta
entrega a una rama para revisarla mediante PR. El merge queda a cargo del usuario;
no se autoriza aplicar migraciones.

El usuario autorizó el paso 9.4 con acceso exclusivo de Administrador y Empleado
en todas las rutas, validación de datos, búsqueda, paginado estable, soft delete,
pruebas y actualización de este documento. Se modifican ProveedoresController y
ProveedorDtos y se agrega ProveedorTests. Se reutilizan CuitUnico e índices sin
modificar entidades ni generar migraciones. La autorización cubre implementación
local y pruebas. Posteriormente el usuario autorizó el commit y la subida de esta
entrega a una rama para revisarla mediante PR. El merge queda a su cargo.

El usuario autorizó el paso 9.5 con el alcance de la sección 11.4: controlador,
DTOs, servicios de imágenes, almacenamiento/configuración, dependencias, pruebas y
este documento. Se conservan stock, históricos y esquema. La autorización cubre
implementación local y pruebas. Posteriormente autorizó el commit y la subida de
esta entrega a una rama para revisión mediante PR. El merge queda a su cargo.
Después de la validación el usuario autorizó retirar todas las pruebas que habían
pasado; se elimina el proyecto tests/Auth.Integration y su referencia en la solución.
No se agregan nuevamente esas pruebas por suposición en los siguientes bloques;
sus resultados quedan registrados en este documento.

Antes del paso 10 revisar encabezados, detalles, permisos de Empleado,
proveedores/productos activos, identidad desde JWT, confirmación, precios y
descuentos históricos, totales y redondeo, reversión y concurrencia de stock.
Presentar alcance concreto antes de editar; no implementar salidas ni carrito
dentro del mismo bloque.

## 16 Historial de entregas

Estado verificado al preparar esta versión: PR #1 al #9 integrados; PR #10 abierto.
Los hashes de la columna Commit identifican el trabajo de cada rama.

| Entrega | Rama | Commit | PR |
| --- | --- | --- | --- |
| Modelo inicial | `reestructuracion/paso-1-modelo` | `d4d237d` | [#1](https://github.com/LuAlzuri/TPN-5-Movil-Correcion/pull/1) |
| Integración | `reestructuracion/paso-7-integracion` | `81ae24d` | [#2](https://github.com/LuAlzuri/TPN-5-Movil-Correcion/pull/2) |
| Autenticación | `reestructuracion/paso-8-autenticacion` | `273585d` | [#3](https://github.com/LuAlzuri/TPN-5-Movil-Correcion/pull/3) |
| Categorías | `codex/paso-9-1-categorias` | `f900a72` | [#4](https://github.com/LuAlzuri/TPN-5-Movil-Correcion/pull/4) |
| Empleados | `codex/paso-9-2-empleados` | `2326368` | [#5](https://github.com/LuAlzuri/TPN-5-Movil-Correcion/pull/5) |
| CUIT/CUIL único | `codex/cuil-unico-clientes-proveedores` | `04fcb22` | [#6](https://github.com/LuAlzuri/TPN-5-Movil-Correcion/pull/6) |
| Documentación unificada | `codex/documentacion-unificada` | `0d99f59` y `29fe5f6` | [#7](https://github.com/LuAlzuri/TPN-5-Movil-Correcion/pull/7) |
| Clientes | `codex/paso-9-3-clientes` | `e1a12b7` y `535e869` | [#8](https://github.com/LuAlzuri/TPN-5-Movil-Correcion/pull/8) |
| Proveedores | `codex/paso-9-4-proveedores` | `ad4267a` y `2a43284` | [#9](https://github.com/LuAlzuri/TPN-5-Movil-Correcion/pull/9) |
| Productos e imágenes y retiro de pruebas | `codex/paso-9-5-productos-imagenes` | `9929183` inicial | [#10 abierto](https://github.com/LuAlzuri/TPN-5-Movil-Correcion/pull/10) |

El PR #9 ya está integrado mediante `c81dad3`, comprobado al iniciar el paso 9.5.
El paso 9.5 se publicó en `codex/paso-9-5-productos-imagenes`, basada en
`origin/main` con ese merge, mediante el commit inicial `9929183`. El PR #10 queda
abierto para revisión; el merge sigue a cargo del usuario. Esta actualización del
historial se publica como seguimiento documental en el mismo PR.

## 17 Fuentes originales y decisiones que las reemplazan

Esta consolidación incorpora las reglas y el plan de
`Contexto_TPN5_Codex_Local.docx`, suministrado desde Downloads, y la documentación
por etapas que estaba en `docs`. Las decisiones posteriores del usuario, como
el catálogo de categorías para los tres roles y la unicidad independiente de
CUIL en clientes/proveedores, están integradas en las secciones vigentes.

Los seis Markdown anteriores quedan sustituidos por este archivo. Sus versiones
anteriores pueden recuperarse del historial Git; no deben seguir actualizándose
como fuentes paralelas:

- `PASO-1-MODELO.md`.
- `PASO-7-INTEGRACION.md`.
- `PASO-8-AUTENTICACION.md`.
- `PASO-9-1-CATEGORIAS.md`.
- `PASO-9-2-EMPLEADOS.md`.
- `CUIL-UNICO-CLIENTES-PROVEEDORES.md`.

Se conservan como archivos originales de referencia, no como documentación
funcional vigente independiente:

- `Alzuri-Fuentes_Docu_API_TP5.pdf`, informe de 33 páginas del trabajo original de
  Facundo Fuentes y Luciana Alzuri, profesor Federico Trani. Contiene pruebas y
  capturas de Postman de la versión anterior.
- `Diagrama-ER.png` del repositorio y el DER externo
  `Copia de Movil.drawio.svg` suministrado desde Downloads. El DOCX mencionaba el
  nombre anterior `Copia de Movil.drawio (1).svg`. El esquema y las decisiones
  posteriores prevalecen ante diferencias; no agregar proveedor directo ni
  ImagenUrl persistida a Producto porque aparezcan en un diagrama anterior.
- `docs/migrations-legacy/*.cs.txt`, fuentes históricas de las migraciones
  Inicial y su snapshot. Son código archivado fuera de compilación, no pasos de
  instalación ni migraciones vigentes que deban aplicarse.

El PDF cubría Auth, Categorías, Proveedores, Clientes, Productos, Ingresos, Salidas
y Usuarios. Su propósito de probar login, Bearer, CRUD, filtros, errores y stock
se conserva en las verificaciones actuales. Sus ejemplos y capturas no garantizan
el comportamiento de la API reestructurada. Estas diferencias están resueltas:

| Descripción histórica | Decisión o estado vigente |
| --- | --- |
| Roles Admin/Usuario | Administrador/Empleado/Cliente, con un rol por cuenta |
| Registro con respuesta 200 | Registro transaccional con respuesta 201 |
| Lecturas generales de categorías para cualquier cuenta | Solo catálogo mínimo para los tres roles; administración y detalle exclusivos de Administrador |
| Proveedor obligatorio y directo en Producto | Proveedor a través de ingresos; Producto solo referencia Categoria |
| Crear ingreso/salida altera stock inmediatamente | Flujo por encabezados, detalles y confirmaciones, todavía pendiente |
| Impedir eliminar categorías, proveedores, clientes o productos por tener relaciones | Soft delete conservando históricos; ingresos confirmados requieren reversión atómica según su regla propia |
| Usuario con Activo editable | Usuario.Eliminado, sin restauración ni cambio de rol en edición común |
| CRUD de clientes o carga de imágenes de la versión anterior | Los pasos 9.3 y 9.5 completan clientes e imágenes con los contratos actuales |
| URL pública usada en las capturas | Referencia histórica; no asumir despliegue actual ni usarla como destino de pruebas sin verificar |

No se copian tokens, credenciales ni capturas de autenticación del informe
histórico. Tampoco se reactiva su antiguo contrato de stock o sus permisos.

## 18 Cómo mantener este archivo

En cada próximo bloque, actualizar **este mismo archivo** en lugar de crear un
Markdown separado por paso. Mantener el README como enlace de entrada.

Antes de editar código, registrar el alcance aprobado y las decisiones que cambien
reglas o permisos. Al cerrar el bloque, actualizar estado, contratos, validaciones,
modelo/migraciones, pruebas, limitaciones y próximo paso. Diferenciar siempre
trabajo propuesto, autorizado, implementado localmente, publicado e integrado.

Si cambia el esquema, documentar los nombres reales de las migraciones, su efecto
sobre datos existentes y si se aplicaron, dónde y con qué autorización. Generar
SQL no equivale a haberlo ejecutado; integrar un PR tampoco confirma que la base
personal esté migrada.

Registrar resultados de pruebas con proveedor y omisiones, sin convertir evidencia
histórica en una validación del código nuevo. Actualizar la tabla de entregas al
crear commits/PR o comprobar su integración. Conservar decisiones superadas solo
como historial claramente marcado y eliminar afirmaciones obsoletas de las
secciones de estado vigente.

No incorporar secretos ni dar por autorizadas operaciones futuras por estar
descritas aquí. Cualquier cambio posterior indicado por el usuario prevalece y
debe reflejarse en este documento.
