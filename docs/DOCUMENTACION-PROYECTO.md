# Documentación del proyecto de inventario TPN 5

Este archivo reúne el contexto de trabajo, las reglas de negocio, el diseño del
modelo, los contratos implementados, las instrucciones de entorno, las migraciones,
las pruebas y el plan de continuidad de la API. Es la documentación vigente que se
debe leer al retomar el proyecto y actualizar en los próximos bloques.

Última actualización: 27 de octubre de 2026, zona America/Buenos_Aires.
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

**Mejoras posteriores — bloque 1:** implementado localmente en
`mejoras/proteger-ultimo-administrador`, desde main `1c2761d` (PR #15 integrado).
El usuario autorizó proteger al último Administrador activo sin cambiar el bootstrap
ni impedir la autoeliminación cuando queda otro Administrador válido. El controlador
delega ahora en AuthService, con revalidación del actor y transacción Serializable.
Resultado: 186 comprobaciones temporales aprobadas en SQL Server, cero fallidas,
incluidas quince carreras de eliminación. No se incorporaron pruebas al repositorio.
No hay cambios de esquema ni migración nueva. Después de validar el bloque, el
usuario autorizó registrar y subir esta entrega por rama para revisión previa al
merge, que queda a su cargo. El siguiente bloque de implementación es limitar intentos de
login, con su propio alcance y autorización. Consultar sección 14.4.

**Actualización del bloque 12.3 (27 de octubre de 2026):** se completó la validación
funcional en SQL Server 2022/Linux desde `dc37184` y se corrigieron dos diferencias
reales: el carrito ahora limita la cantidad a stock y acepta cero como retiro de
línea; los deadlocks/lock timeouts envueltos por EF Core se traducen a 409.
El bloque 12.3 se entregó en `validacion/paso-12-3`, commit `54867bc`, y el usuario
integró el PR #15 en main mediante `1c2761d`. No se modificaron bases personales.

Resultado al cierre del bloque 12.3: 454 comprobaciones funcionales aprobadas, cero fallidas, en doce
escenarios, más siete comprobaciones de actualización de migraciones aprobadas.
Las pruebas se ejecutaron fuera del repositorio contra bases y almacenamiento
exclusivos de una instancia SQL Server descartable. No se modificó ninguna base
personal. No se agregó una suite de pruebas al repositorio ni una migración vacía.
Consultar la sección 14.3 para alcance y límites.

La última suite versionada aprobó 165 pruebas en SQLite aislada, con 0 fallidas y
0 omitidas. Después de esa ejecución el usuario pidió eliminar todas las pruebas
exitosas: se retiraron la carpeta tests y su proyecto de la solución. Los resultados
se conservan como evidencia histórica, sin una suite versionada actual.
El registro transaccional, la autenticación y los CRUD de
categorías, empleados, clientes, proveedores y productos/imágenes están implementados.
El paso 10 de ingresos y stock está publicado en el PR #11 y validado mediante
242 comprobaciones temporales, sin volver a incorporar pruebas al repositorio.
El PR #11 está integrado en main mediante `6e35de1`, verificado al iniciar el paso 11.
Los resultados figuran en la sección 14.
La unicidad de CUIT/CUIL entre clientes y entre proveedores tiene migraciones propias.

Los PR del 1 al 14 están integrados en `main`, verificado al iniciar el bloque
12.3. El último es el PR #14, integrado mediante `dc37184`. Verificar Git y GitHub de nuevo en cada sesión: este dato no se actualiza
automáticamente por estar escrito aquí.

El usuario autorizó el **paso 11, flujo completo de salidas y carrito**, incluida
la confirmación por Administrador y Empleado y la referencia del confirmador a Usuario.
Está publicado en `codex/paso-11-salidas-stock` mediante el PR #12 y validado mediante
361 comprobaciones temporales aprobadas, sin incorporar pruebas al repositorio.
El PR #12 fue integrado por el usuario mediante `2775d66`. En ese bloque no se
aplicó la migración; en 12.3 se ejecutó únicamente en bases descartables de prueba.
El bloque **12.1, completar y corregir Usuarios**, está autorizado, implementado
y publicado en `codex/paso-12-1-usuarios` mediante el PR #13: 293 comprobaciones
temporales aprobadas y compilación sin errores ni advertencias. El PR #13 está integrado mediante `7269ef6`.
El usuario autorizó el bloque **12.2 de consultas administrativas de históricos y
eliminados**. Está publicado en `codex/paso-12-2-auditoria` mediante el PR #14:
494 comprobaciones temporales aprobadas, compilación sin errores ni advertencias
y traducción a SQL Server comprobada sin conexión. El PR #14 fue integrado por el usuario. La validación funcional integral en SQL
Server se completó en el bloque 12.3 y el usuario autorizó su entrega por rama
para revisión previa al merge.

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
| Productos e imágenes | Paso 9.5 integrado en PR #10 |
| Ingresos y stock | Paso 10 integrado en PR #11 |
| Salidas, carrito y stock | Paso 11 integrado en PR #12 |
| CRUD general de usuarios | Bloque 12.1 integrado en PR #13 |
| Consultas de históricos y eliminados | Bloque 12.2 integrado en PR #14 |
| Base personal del usuario | No se verificó ni migró; no asumir que está preparada |

POST /api/usuarios deja de devolver 501 en el bloque 12.1. Los tres tipos de cuenta
pueden crearse mediante `POST /api/usuarios` o `POST /api/auth/register`, exclusivos
de Administrador y con la misma creación transaccional de cuenta y perfil. Los empleados y clientes también pueden crearse
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
| Salida | Id, Fecha, Observaciones, ClienteId, UsuarioConfirmacionId opcional hasta confirmar (FK a Usuario), ConfirmarXCliente, ConfirmarXEmpleado, FechaConfirmacionCliente, FechaConfirmacionEmpleado, Eliminado; varios DetalleSalida |
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
| CRUD general de usuarios | Sí | No | No |
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

El paso 10 implementa ingresos y el paso 11 implementa salidas y carrito según
las reglas de esta sección.

### 5.1 Reglas comunes

- Stock mínimo 0: se permite agotarlo, nunca dejarlo negativo ni vender sin disponibilidad.
- Cantidad de cada detalle: mínimo 1. En ventas/salidas, la cantidad total solicitada
  de un producto no puede superar el stock disponible. Los ingresos pueden crearse
  con stock actual 0 y lo incrementan al confirmar, sin exceder Int32.
- DescAplicado es un porcentaje de 0 a 100.
- Guardar PrecioUnitario en el detalle para conservar el histórico aunque cambie
  el precio del producto.
- Subtotal previsto: `Cantidad × PrecioUnitario × (1 − DescAplicado / 100)`.
  El total del encabezado es la suma de subtotales ya redondeados. Para ingresos
  se acordó redondear cada subtotal a dos decimales con MidpointRounding.AwayFromZero.
  Los precios unitarios se capturan desde Producto al agregar el detalle y se
  conservan al editar cantidad/descuento. Subtotal y total no exceden decimal(18,2)
  en las nuevas escrituras. Los DTOs exponen estos cálculos sin columnas nuevas.
- Importes configurados como `decimal(18,2)` y descuentos como `decimal(5,2)`.
- Registrar fecha, proveedor o cliente y empleado según corresponda.
- Las operaciones que alteren stock requieren transacciones explícitas
  `IDbContextTransaction`. Una transacción por sí sola no evita todas las carreras:
  usar actualizaciones condicionadas de estado y stock y comprobar filas afectadas.
- Evitar aplicar o revertir stock más de una vez, sin incorporar RowVersion.

### 5.2 Ingresos

Flujo implementado en el paso 10. El empleado crea el encabezado sin confirmar. El encabezado no se edita. Sus
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
sus propias salidas. Obtener ClienteId y UsuarioConfirmacionId del JWT validado cuando
corresponda; no aceptarlos desde el JSON para asignar identidad.

Cada producto tiene un solo detalle por salida. Cambiar cantidades actualiza el
detalle existente. No persistir cantidad cero o negativa. Desde el bloque 12.3,
POST/PUT con cantidad cero retira la línea y responde 204; DELETE sigue disponible.
Una cantidad negativa se rechaza con 400. Con stock 5 se puede solicitar hasta 5,
nunca 6. Se valida al agregar, editar y solicitar, y nuevamente al confirmar la venta.

El carrito no reserva ni descuenta stock. Al confirmar el cliente
(`ConfirmarXCliente = true`), se bloquean sus detalles y la solicitud queda
disponible para confirmación operativa.

Administrador y Empleado pueden confirmar solamente después del cliente: se
comprueba nuevamente el stock, se registra el UsuarioConfirmacionId desde el JWT y
la fecha UTC y se descuenta todo en una transacción. Si falla un producto, se
revierte toda la operación. UsuarioConfirmacionId referencia Usuarios con Restrict;
los administradores mantienen su único rol y no reciben perfil Empleado.
Los nombres históricos ConfirmarXEmpleado y FechaConfirmacionEmpleado representan
la confirmación operativa de cualquiera de esos dos roles.
El cliente puede eliminar su salida mientras no esté confirmada por el empleado,
incluso si él ya la solicitó. Después de la confirmación operativa no puede eliminarla.
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
y EmpleadoId del principal validado. Salidas usa ObtenerUsuarioId para su confirmador
y ObtenerClienteId para la propiedad del carrito. CORS sigue
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

### 11.2 Usuarios completados en el bloque 12.1

Todas las rutas de UsuariosController requieren Administrador. GET devuelve solo
cuentas activas. El listado valida page positivo, pageSize 1..100 y desplazamiento
dentro de int; ordena por Email e Id, recorta busqueda y filtra por nombre completo
o email. Una página fuera de rango queda vacía; una búsqueda vacía no filtra.
GET por ID devuelve UsuarioDto o 404; no expone Password ni PasswordHash.

POST /api/usuarios devuelve 201 y Location al detalle. Usa AuthService.RegisterAsync,
con la misma transacción que crea Usuario y el perfil de PK compartida. No se
implementa otra creación paralela ni se expone autorregistro público. El DTO exige
Nombre/Apellido hasta 150, CUIT hasta 20, Email válido hasta 150 y contraseña entre
12 y 128 caracteres, no vacía ni solo espacios. El email se recorta y pasa a
minúsculas antes de validarlo; nombres/apellido/CUIT se recortan antes de guardar.
La longitud de email se evalúa normalizada; las otras longitudes se validan sobre
el valor recibido. Se rechazan campos desconocidos, incluidos Id, Eliminado,
PasswordHash, objetos de perfil, stock o múltiples roles.

La creación requiere exactamente Rol Administrador, Empleado o Cliente:
Administrador no admite datos de perfil; Empleado requiere FechaIngreso distinta
de MinValue y no admite Telefono/Direccion; Cliente admite Telefono hasta 50 y
Direccion hasta 300, recortados o null si están vacíos, sin FechaIngreso. Los datos
de un perfil incompatible se rechazan aunque sean cadenas vacías o espacios.
Las reglas de perfil y contraseña se vuelven a validar en AuthService.

PUT actualiza exclusivamente Nombre, Apellido, CUIT, Email y Password opcional,
con las mismas validaciones de campos comunes. Omisión o null de Password conserva
el hash; si se suministra un valor, se exige la política de 12..128 caracteres y
no solo espacios. Un valor vacío ya no se ignora: devuelve 400. Una contraseña
nueva se hashea con PasswordHasher y revoca los JWT anteriores mediante la
validación existente. El rol, los perfiles y sus datos específicos no se modifican
ni se aceptan en ese JSON. Los datos desconocidos se rechazan, también Eliminado.

La comparación de email al editar ignora mayúsculas y contempla cuentas eliminadas,
excluyendo el ID editado. AuthService conserva esa regla al crear. CUIT/CUIL mantiene
unicidad solo entre Clientes, incluyendo eliminados y normalizando los separadores
definidos en la sección 10; no se amplía la unicidad a Empleados/Administradores ni
se agrega formato de once dígitos o validación de dígito verificador. Los conflictos
SQL Server de claves únicas 2601/2627 siguen traduciéndose a 409.

DELETE devuelve 204 y marca Usuario.Eliminado: conserva perfiles y referencias
históricas, revoca el JWT de la cuenta y no restaura ni borra físicamente datos.
Una cuenta ausente o eliminada devuelve 404 al consultar, editar o eliminar de nuevo.
El bloque posterior de protección del último Administrador agrega una condición:
si el objetivo es Administrador, debe quedar otro Administrador activo y con perfil
consistente (sin subperfil Cliente/Empleado). Si no queda, responde 409 sin cambios.
La autoeliminación sigue permitida cuando queda ese respaldo. El actor se obtiene
del JWT y se revalida dentro de la misma transacción Serializable. Actores que ya
no están activos o son inconsistentes se rechazan con 403; un token eliminado se
rechaza normalmente antes, con 401. Los conflictos 1205/1222, incluso envueltos por
EF Core, responden 409 sin reintentos automáticos. No cambia el bootstrap, roles,
restauración, perfiles, tablas, índices ni migraciones. UsuariosController delega
la eliminación en AuthService; el resto de contratos conserva su implementación.

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
activo al crear y confirmar un ingreso está implementada en el paso 10. El borrado
del proveedor no impide eliminar un borrador ni revertir un ingreso histórico.

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

### 11.5 Ingresos y stock implementados en el paso 10

| Método | Ruta | Permisos y comportamiento |
| --- | --- | --- |
| GET | `/api/ingresos` | Administrador y Empleado; listado paginado |
| GET | `/api/ingresos/{id}` | Administrador y Empleado; encabezado, detalles y total |
| GET | `/api/ingresos/{id}/detalles/{detalleId}` | Administrador y Empleado; detalle perteneciente al encabezado |
| POST | `/api/ingresos` | Empleado; crea borrador y devuelve 201 con Location |
| POST | `/api/ingresos/{id}/detalles` | Empleado; agrega detalle y devuelve 201 con Location |
| PUT | `/api/ingresos/{id}/detalles/{detalleId}` | Empleado; cambia cantidad/descuento y devuelve 200 con detalle calculado |
| DELETE | `/api/ingresos/{id}/detalles/{detalleId}` | Empleado; quita un detalle de borrador; 204 |
| POST | `/api/ingresos/{id}/confirmar` | Empleado; confirma y suma stock; 200 con encabezado |
| DELETE | `/api/ingresos/{id}` | Empleado; soft delete y reversión si corresponde; 204 |

No hay PUT del encabezado. El Administrador no hereda permisos de escritura;
Cliente no accede a estas rutas. Un empleado activo puede operar sobre ingresos
creados por otro empleado: se conservan creador y confirmador por separado.
El ID operativo se obtiene del JWT mediante UsuarioAutenticado y se verifica
nuevamente el perfil activo dentro de la transacción. No se toma de JSON.

IngresoRequest admite únicamente ProveedorId positivo y Observaciones opcionales
de hasta 1000 caracteres, recortadas y vacías como null. Fecha UTC, empleado creador,
estado y fechas de confirmación son del servidor. El proveedor debe estar activo
al crear. Un encabezado nuevo no tiene detalles, total 0 ni impacto sobre stock.

DetalleIngresoCreateRequest admite ProductoId positivo, Cantidad obligatoria de
1 a Int32.MaxValue y DescAplicado opcional (0 por defecto). El producto debe estar
activo; su precio actual se copia como PrecioUnitario histórico. El descuento está
entre 0 y 100 con hasta dos decimales. Editar solo recibe Cantidad y DescAplicado,
sin cambiar ProductoId ni PrecioUnitario; omitir descuento lo establece en 0.
Los DTOs rechazan propiedades desconocidas, incluyendo identidades, estados y
precio aportado por el cliente. Las mismas reglas se validan en el servicio.

Los detalles admiten varias filas de un mismo producto en un ingreso, a diferencia
del carrito de salida. Pueden reflejar precios capturados en momentos distintos.
Al confirmar/revertir se agrupan sus cantidades por producto, usando long para
sumar y rechazando acumulados fuera de Int32. Se actualizan productos por ID
ascendente para mantener un orden de bloqueo uniforme.

DetalleIngresoResponse agrega Subtotal e IngresoResponse agrega Total a sus campos
anteriores. Subtotal = Cantidad × PrecioUnitario × (1 − DescAplicado/100), redondeado
a dos decimales con AwayFromZero. Total suma esos subtotales, no redondea primero
la suma sin redondear. Los nuevos detalles y ediciones rechazan importes que
excedan 9999999999999999.99; no se cambia esquema ni se persisten totales derivados.

Confirmar exige al menos un detalle, proveedor activo y productos activos. Dentro
de una transacción registra Confirmado, FechaConfirmacion UTC y el empleado
confirmador, y suma todo el stock. Cada actualización exige producto activo y
Stock <= Int32.MaxValue − cantidad. Si una fila no cambia, todo se revierte.
Los detalles quedan bloqueados; reconfirmar o modificar detalles confirmados
devuelve 409. Cambiar precios del catálogo no recalcula los detalles históricos.

Quitar un detalle de borrador lo elimina físicamente, sin afectar stock; los
detalles confirmados nunca se quitan. Eliminar un encabezado marca Eliminado y
conserva detalles. Si estaba confirmado, revierte stock con condición Stock >=
cantidad. La reversión también alcanza productos eliminados lógicamente para no
perder consistencia histórica. Un stock insuficiente o producto inexistente
rechaza toda la eliminación, incluida cualquier reversión previa del mismo lote.
Repetir la eliminación devuelve 404 sin volver a tocar stock. No se borran perfiles,
proveedores, productos ni datos de confirmación históricos.

IngresoService usa IDbContextTransaction explícitas con aislamiento Serializable
en todas las escrituras. Antes de operar realiza una actualización sin cambio de
Observaciones en el encabezado activo para mantener su bloqueo hasta el commit.
Luego comprueba el estado y hace actualizaciones condicionadas de Confirmado,
Eliminado y Stock, verificando filas afectadas. Eso serializa edición, confirmación
y reversión de un mismo ingreso. No se usa RowVersion ni columnas nuevas. Los
deadlocks SQL Server 1205 y los lock timeouts 1222 devuelven 409 para reintentar;
las excepciones o cancelaciones anteriores al commit revierten la transacción.
Si se pierde la respuesta de un commit real, el estado persistido impide una
segunda aplicación/reversión al reintentar el mismo encabezado.

El listado valida page positivo, pageSize de 1 a 100 (1 y 10 por defecto), filtros
productoId/proveedorId positivos y rango desde/hasta coherente. Ordena por Fecha
e Id descendentes; una página fuera del resultado queda vacía. Listado y detalle
conservan encabezados activos y todos sus detalles aunque sus contrapartes o
productos históricos estén eliminados. No se agregan endpoints de auditoría de
ingresos eliminados en este bloque. Un detalle de otro ingreso o encabezado
eliminado no se puede leer/modificar mediante una ruta ajena.

### 11.6 Salidas y carrito implementados en el paso 11

| Método | Ruta | Permiso y resultado |
| --- | --- | --- |
| GET | `/api/salidas` | Tres roles; Cliente solo sus salidas; paginado y filtros |
| GET | `/api/salidas/{id}` | Tres roles; Cliente solo su encabezado, detalles y total |
| GET | `/api/salidas/{id}/detalles/{detalleId}` | Tres roles; pertenencia y propiedad comprobadas |
| POST | `/api/salidas` | Cliente; crea su encabezado desde JWT; 201 y Location |
| POST | `/api/salidas/{id}/detalles` | Cliente propietario; crea detalle 201 con Location, actualiza cantidad 200 o retira con cantidad cero 204 |
| PUT | `/api/salidas/{id}/detalles/{detalleId}` | Cliente propietario; reemplaza cantidad; 200, o 204 si cero retira la línea |
| DELETE | `/api/salidas/{id}/detalles/{detalleId}` | Cliente propietario; quita físicamente un detalle en borrador; 204 |
| POST | `/api/salidas/{id}/confirmar-cliente` | Cliente propietario; solicita la salida y bloquea detalles; 200 |
| POST | `/api/salidas/{id}/confirmar` | Administrador o Empleado; confirma y descuenta stock una vez; 200 |
| DELETE | `/api/salidas/{id}` | Cliente propietario; borrado lógico antes de confirmación operativa; 204 |

No hay PUT del encabezado. El cliente recibe 404 al intentar leer o modificar una
salida ajena, incluso sus detalles; no puede ampliar su listado con clienteId.
Administrador y Empleado consultan todas las salidas activas y sus detalles, pero
no crean ni modifican carritos de clientes. Los actores salen del JWT; no se
aceptan desde los JSON de encabezado/detalle. Las confirmaciones no enlazan un
body: un JSON adicional no decide actor, cantidades ni estados.

SalidaRequest solo admite Observaciones, opcional, máximo 1000, recortada o null.
DetalleSalidaCreateRequest exige ProductoId positivo y Cantidad no negativa; Update
solo Cantidad. Los detalles persistidos mantienen Cantidad >= 1. Campos desconocidos se rechazan, incluidos precio, descuento, cliente,
confirmador y estado. Cantidad cero retira la línea con 204; negativa se rechaza. DELETE sigue disponible.
POST con cero sin línea previa responde 204 sin crear registros.
Puede haber varios carritos por cliente; no se impone un único carrito abierto.

Un producto ocupa un solo detalle por salida (índice único conservado). POST sobre
un producto existente reemplaza su cantidad, conserva su Id, precio y descuento,
y devuelve 200; no suma ni duplica filas. Al agregar por primera vez se captura el
precio actual de Producto y se fija DescAplicado en 0. El cliente no asigna precios
ni descuentos. Editar cantidades o confirmar no recalcula el precio histórico.
Quitar y volver a agregar captura un precio nuevo. Subtotal y Total se calculan
igual que en ingresos: redondeo por detalle AwayFromZero a dos decimales y suma de
subtotales, con límites decimal(18,2). No hay columnas nuevas para importes.
Los descuentos históricos válidos se conservan; no se agrega edición de descuentos.

Crear, editar y confirmar por cliente no reserva ni descuenta existencias; se
rechaza solicitar más unidades que el stock actual con 409. Ambas confirmaciones exigen
al menos un detalle y productos activos. Solicitar bloquea altas, cambios y bajas
de detalles. Repetir cualquiera de las confirmaciones devuelve 409.

La confirmación operativa exige cliente activo y consistente, solicitud previa y
estado pendiente. Usa transacción Serializable, actualización sin cambio del
encabezado para bloquearlo y condiciones de estado/stock con comprobación de filas
afectadas. Actualiza productos por Id ascendente, exigiendo Stock >= Cantidad.
Cualquier producto sin stock, eliminado o fallo intermedio revierte estado, actor,
fecha y todos los descuentos de stock. No hay confirmación parcial ni stock negativo.
Las escrituras revalidan el perfil activo del actor dentro de la transacción.
Los conflictos SQL Server 1205/1222 se devuelven como 409 para reintentar,
incluso cuando EF Core los envuelve en InvalidOperationException. No se reintentan
movimientos automáticamente.

DELETE del encabezado antes de confirmar operativamente conserva detalles y estados
históricos, sin mover stock, incluso si ya lo solicitó el cliente. Repetir DELETE
retorna 404. Una salida confirmada operativamente devuelve 409 al intentar eliminarla;
no se incorpora cancelación ni reposición posterior. Las consultas conservan los
históricos de productos, clientes o confirmadores eliminados, sin exponer salidas
eliminadas. Los movimientos de productos existentes incluyen las salidas confirmadas.

El listado valida page positivo, pageSize 1..100, desplazamiento dentro de int,
identificadores de filtro positivos y desde <= hasta. Orden Fecha/Id descendente.
ProductoId, ClienteId y fechas se combinan con la propiedad obligatoria del cliente.
Una página fuera del resultado es vacía. Se elimina el servicio provisional
MovimientoService y su respuesta 501; SalidaService gestiona el flujo completo.

### 11.7 Consultas de auditoría del bloque 12.2

Solo Administrador puede acceder a estas rutas GET. Empleado y Cliente reciben
403 y las peticiones sin JWT, 401. No hay escrituras, restauración ni borrado en
este controlador. Se agregan AuditoriaController, AuditoriaDtos, AuditoriaService
y su registro en Program; no cambian entidades, migraciones, dependencias ni los
permisos y filtros de las rutas operativas.

Cada recurso de esta lista tiene GET `/api/auditoria/{recurso}` paginado y GET
`/api/auditoria/{recurso}/{id}` individual:

| Recurso | Información y filtros específicos del listado |
| --- | --- |
| usuarios | Datos públicos de cuenta, rol, Eliminado y presencia de perfiles; busqueda y rol exacto Administrador/Empleado/Cliente |
| empleados | Cuenta, FechaIngreso, Eliminado heredado de Usuario y PerfilConsistente; busqueda |
| clientes | Cuenta, Telefono, Direccion, Eliminado heredado de Usuario y PerfilConsistente; busqueda |
| categorias | Nombre, Descripcion, FechaCreacion y Eliminado; busqueda |
| proveedores | Nombre, CUIT, contacto y Eliminado; busqueda |
| productos | Datos del producto, stock actual, categoría con su estado e imagen actualmente asociada; busqueda y categoriaId |
| ingresos | Resumen con proveedor, creador, confirmador, estados y fechas; proveedorId, productoId, confirmado, desde/hasta |
| salidas | Resumen con cliente, confirmador, estados y fechas; clienteId, productoId, confirmarXCliente, confirmarXEmpleado, desde/hasta |

Todos los listados admiten eliminado opcional: omitido incluye activos y eliminados,
true solo eliminados, false solo activos. Este filtro se aplica al recurso raíz;
un producto activo con categoría eliminada sigue activo y conserva esa referencia.
En empleados/clientes se aplica a Usuario.Eliminado. Se muestran perfiles existentes
incluso si son inconsistentes con el rol; PerfilConsistente los identifica sin
corregir ni ocultar datos. No se incluyen Password ni PasswordHash en ninguna respuesta.

Page debe ser positivo, pageSize estar entre 1 y 100, el desplazamiento caber en int
y los IDs de filtro ser positivos. Busqueda admite hasta 300 caracteres, se recorta
y si queda vacía no filtra. En cuentas/perfiles busca por nombre completo, email o
CUIT; categorías/productos, por nombre o descripción; proveedores, nombre/CUIT/email.
Rol y Tipo de movimiento se recortan, admiten vacío como ausencia y exigen los valores
exactos documentados; los inválidos devuelven 400. Los booleanos, números y fechas
mal formados también devuelven 400. Los rangos desde/hasta son inclusivos, coherentes
y se aplican a Fecha de creación del encabezado, no a fechas de confirmación.

Usuarios ordena por Email e Id; perfiles por Apellido, Nombre e Id; catálogos por
Nombre e Id; ingresos/salidas por Fecha e Id descendentes. Una página fuera del
resultado es vacía y un registro ausente devuelve 404. El detalle individual no
se limita por el filtro de eliminación del listado.

Los listados de ingresos/salidas devuelven resúmenes sin cargar sus detalles. La
consulta individual devuelve Encabezado, Detalles y Total. Cada detalle conserva
ID, producto con su estado actual, Cantidad, PrecioUnitario, DescAplicado y Subtotal.
Los importes usan decimal y redondeo por detalle AwayFromZero a dos decimales,
igual que los flujos operativos. Total suma subtotales ya redondeados. Proveedor,
cliente, empleados y usuario confirmador se consultan aunque estén eliminados;
los responsables muestran ID, nombre, apellido, rol actual y estado. En ingresos,
Revertido deriva de Confirmado y Eliminado conforme al flujo autorizado del paso 10.

GET `/api/auditoria/productos/{id}/movimientos` incluye productos activos o eliminados
y los detalles conservados de ingresos/salidas en cualquier estado. Admite page,
pageSize, eliminado (del encabezado), tipo Ingreso/Salida y desde/hasta. Ordena por
Fecha descendente, Tipo, EncabezadoId y DetalleId, y pagina en SQL.

| Campo de movimiento | Significado |
| --- | --- |
| EncabezadoId, DetalleId, ProductoId, Tipo | Identifican inequívocamente el detalle y su origen |
| Fecha y FechaConfirmacion | Creación y confirmación operativa, si existe; no fecha de eliminación |
| Eliminado y Estado | Ingreso: Borrador, Confirmado, BorradorEliminado o Revertido; Salida: Borrador, Solicitada, Confirmada o Eliminada |
| Cantidad | Cantidad del comprobante con signo: ingreso positivo, salida negativa, también en borradores |
| ImpactoStockVigente | Cantidad firmada solo si está confirmado operativamente y no eliminado; cero en el resto |
| PrecioUnitario, DescAplicado, Subtotal | Valores conservados y subtotal positivo calculado sobre cantidad absoluta |
| Contraparte | Proveedor o cliente y su estado actual de eliminación |

ImpactoStockVigente expresa el aporte del comprobante según el estado almacenado y
las reglas operativas; no es un saldo acumulado ni reconstruye existencias en una
fecha pasada. Un ingreso revertido conserva cantidad/precio históricos, pero su
aporte vigente es cero. No se crea otro movimiento ficticio de reversión ni se
inventa su fecha. La confirmación y la eliminación siguen siendo operaciones de
los flujos originales, sin ejecutarlas desde estas consultas.

El modelo conserva el estado actual de registros y relaciones: nombres, contacto
y roles pueden haber cambiado. PrecioUnitario y DescAplicado sí quedan capturados
en detalles. No registra fecha/autor de cada borrado ni todas las versiones de cada
campo; estas consultas no inventan esos datos. Un detalle de borrador eliminado
físicamente o una imagen reemplazada no se puede recuperar por estas rutas. Los
metadatos de imagen se muestran incluso con producto eliminado; no se leen archivos
físicos ni se crean copias. Las URLs estáticas mantienen el comportamiento público
ya documentado en el paso 9.5.

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

Actualmente hay 14 migraciones: las once iniciales, dos de CUIL y una de Salida:

| Migración | Cambio real |
| --- | --- |
| `20261026210457_CuilUnicoClientes` | CUITNormalizado calculado en Usuarios e índice único filtrado por rol Cliente |
| `20261026210619_CuilUnicoProveedores` | CUITNormalizado calculado en Proveedores e índice único sin filtro |
| `20261027010400_ConfirmadorUsuarioSalida` | Renombra EmpleadoId a UsuarioConfirmacionId y referencia Usuarios; actualiza índice y restricción de confirmación |

La migración del paso 11 conserva los valores existentes: Empleado.Id comparte
clave con Usuario.Id. No elimina ni recrea registros de salidas. El modelo y la API
requieren esta migración antes de usarse juntos. No se aplicó a bases del usuario.
La reversión Down vuelve a exigir FK a Empleados: no es compatible con salidas
confirmadas por administradores sin perfil Empleado; no debe ejecutarse sin revisar
esos datos y acordar cómo conservar su historial. No se borran ni reasignan actores
automáticamente para permitir esa reversión.

No hubo cambios de esquema en los pasos 7, 8, 9.1, 9.2, 9.3, 9.4, 9.5 ni 10; no se crearon migraciones
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
| Paso 10 | 242 comprobaciones temporales aprobadas; compilación sin errores ni advertencias | HTTP, fallos y concurrencia en SQLite aislada; adaptación del descuento para ese proveedor; sin validación funcional en SQL Server ni pruebas añadidas al repositorio |
| Paso 11 | 361 comprobaciones temporales aprobadas; compilación sin errores ni advertencias; 14 migraciones y sin cambios pendientes de modelo | HTTP, fallos y concurrencia en SQLite aislada; SQL de migración generado/revisado sin ejecutarlo; sin validación funcional en SQL Server ni pruebas añadidas al repositorio |
| Bloque 12.1 | 293 comprobaciones temporales aprobadas; compilación sin errores ni advertencias; sin cambios pendientes de modelo | HTTP y JWT reales, rollback y concurrencia en SQLite aislada; sin pruebas funcionales en SQL Server ni pruebas añadidas al repositorio |
| Bloque 12.2 | 494 comprobaciones temporales aprobadas; compilación sin errores ni advertencias; traducción SQL Server sin conexión; sin cambios pendientes de modelo | HTTP con JWT real y datos SQLite aislados; sin pruebas funcionales en SQL Server ni pruebas añadidas al repositorio |

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

En el paso 10 se ejecutó un verificador temporal fuera del repositorio, con JWT
reales de cuentas ficticias, SQLite en archivos separados y conexiones distintas.
Sus 242 comprobaciones aprobaron permisos, DTOs, identidad, encabezado inmutable,
descuentos/redondeo, precio histórico, totales y límites, detalles ajenos, filtros,
confirmación única, eliminación de borrador, reversión, overflow, referencias
eliminadas y rollback por stock insuficiente. Se inyectaron fallos en la segunda
actualización de stock y después de un commit real para verificar rollback o
rechazo del reintento sin duplicar stock. Se corrieron solicitudes concurrentes de
confirmación/confirmación, edición/confirmación, confirmación/eliminación,
eliminación/eliminación y dos ingresos distintos que comparten productos.

El verificador adapta exclusivamente en su DbContext temporal DescAplicado a REAL:
SQLite almacena decimal como TEXT y su CHECK BETWEEN no reproduce decimal(5,2)
de SQL Server para descuentos como 50. La aritmética del servicio siguió usando
decimal; no se cambió el modelo productivo ni la restricción SQL Server. Estas
comprobaciones no certifican los bloqueos ni las migraciones en SQL Server real.
tools/Schema confirmó once entidades y trece migraciones, y dotnet-ef 9.0.11
confirmó que no hay cambios pendientes de modelo, sin conectar a bases. No se
aplicaron migraciones ni se modificó una base personal.

En el paso 11 se ejecutaron 361 comprobaciones temporales externas al repositorio.
Se usaron clientes HTTP con JWT de inicio de sesión real, webroot y archivos SQLite
exclusivos por escenario. Se verificaron permisos por ruta, propiedad y ocultación
de carritos ajenos, paginado/filtros, rechazo de identidades/precios/descuentos en
JSON, detalle único, reemplazo de cantidad y precio histórico, límites monetarios,
redondeo, bloqueo al solicitar, confirmación por ambos roles desde JWT, eliminación
previa a confirmación y persistencia histórica tras bajas lógicas.

Se verificó que solicitar no reserve stock, que la falta de un segundo producto
revierta el primero y que se pueda reintentar tras reponer existencias. Un fallo
inyectado al actualizar el segundo producto revirtió estado y todo el stock; una
respuesta perdida después de commit conservó lo confirmado y rechazó un segundo
descuento. Las carreras incluyeron Administrador/Empleado sobre la misma salida,
confirmación/eliminación, edición/solicitud, dos solicitudes, dos altas del mismo
producto y pedidos distintos compitiendo por existencias insuficientes. Se comprobó
el índice único y la interacción con ingresos: no se permite revertir un ingreso
cuyo stock ya consumió una salida.

Solo el contexto temporal de SQLite convierte DescAplicado de DetalleSalida y
DetalleIngreso a REAL para evitar comparaciones lexicográficas del CHECK con
columnas decimal representadas como TEXT. La API mantiene decimal, sus precisiones
y sus cálculos. Esta adaptación no valida las características de bloqueo de SQL
Server. No se ejecutó la antigua suite retirada ni se afirmó una regresión integral.
La compilación quedó con 0 errores y 0 advertencias; tools/Schema confirmó once
entidades, catorce migraciones, FK del confirmador a Usuario y Restrict sin cascadas.
Dotnet-ef 9.0.11 confirmó ausencia de cambios pendientes del modelo. El SQL generado
renombra columna e índice, actualiza CHECK y FK dentro de una transacción y no
borra registros. No se ejecutó la migración ni se conectó a bases personales o a
SQL Server para validación funcional. El rollback a FK Empleados requiere revisar
previamente las confirmaciones de administradores, como explica la sección 13.1.

En el bloque 12.1 se ejecutaron 293 comprobaciones temporales fuera del repositorio,
mediante HTTP con JWT emitidos por login real y archivos SQLite/webroot aislados.
Se verificaron permisos de las cinco rutas de Usuarios, lecturas sin contraseña,
Location, creación de Administrador/Empleado/Cliente con perfil exacto y PK
compartida, ausencia de perfiles incompatibles, normalización y límites de datos,
campos desconocidos, paginado, búsqueda y rechazo de entradas inválidas sin cambios
persistidos. La creación general conserva la unicidad con /api/auth/register.

La edición conserva el rol y los datos propios del perfil. Se verificaron email
sin distinción de mayúsculas e incluyendo eliminados, CUIL de cliente normalizado
e incluyendo eliminados y su independencia respecto de proveedores y otros roles.
Omitir o enviar null en Password conserva el hash; cambiarla invalida los JWT
anteriores y las credenciales previas. Los valores vacíos o solo espacios se
rechazan. Soft delete oculta la cuenta, revoca su JWT y conserva perfiles y salidas
históricas; no permite reutilizar email ni CUIL de cliente.

Un fallo inyectado al insertar Cliente revirtió también la inserción de Usuario:
no quedó una cuenta huérfana y el reintento posterior creó la cuenta normalmente.
Una respuesta perdida después del commit conservó cuenta/perfil y el reintento
recibió 409. Dos altas concurrentes con los mismos datos produjeron una sola cuenta
y un conflicto, sin duplicados. Son escenarios de SQLite, sin afirmar equivalencia
de bloqueo ni verificar funcionalmente SQL Server. Se reutilizó únicamente la
infraestructura temporal de las verificaciones anteriores; no se ejecutaron de
nuevo sus suites completas ni se añadieron pruebas al repositorio.

La compilación de la solución terminó con 0 errores y 0 advertencias. tools/Schema
confirmó once entidades y catorce migraciones y dotnet-ef 9.0.11 confirmó que no hay
cambios de modelo pendientes. En este bloque no se modificaron entidades, snapshot,
migraciones ni dependencias. No se conectó a bases personales ni se aplicaron
migraciones. Las verificaciones temporales conservan la adaptación de descuentos
SQLite descrita en el paso 11; no cambia el modelo de producción.

En el bloque 12.2 se ejecutaron 494 comprobaciones temporales externas al repositorio,
con JWT reales y archivos SQLite/webroot aislados. Se verificó la autorización de
las diecisiete rutas GET, rechazo de escrituras en listados/detalles, campos y
filtros inválidos, estados omitido/activo/eliminado, búsqueda recortada, orden y
paginación estable, registros ausentes y resultados vacíos. Las comprobaciones
usaron borrados de las API operativas antes de consultar los históricos.

Los escenarios conservaron ingresos/salidas con proveedor, creador, confirmador,
cliente, categoría, producto e imagen asociados a registros eliminados. Se probó
confirmación por empleado y por un administrador eliminado y se consultó todo con
un segundo administrador activo. Se verificaron precios capturados frente al
precio actual del producto, descuentos, redondeo AwayFromZero, totales, resúmenes
y detalles, imágenes opcionales, perfiles inconsistentes visibles y filtros de
referencias/confirmación/fechas inclusive sobre encabezados eliminados.

Se verificaron los ocho estados de los movimientos: cuatro de ingresos y cuatro
de salidas; cantidades firmadas, subtotales positivos, impacto vigente cero de un
ingreso revertido y aporte de una salida confirmada. Se combinaron filtros de
tipo/estado/fechas y se probó paginación de la unión con fechas empatadas.
Las consultas operativas mantuvieron sus 404 para eliminados. Un interceptor
rechazaba cualquier escritura durante la auditoría y no registró ninguna; el
stock permaneció idéntico y el contexto de consulta no siguió entidades. Ningún
JSON expuso Password/PasswordHash ni la contraseña de prueba.

Se comprobó adicionalmente ToQueryString del proveedor SQL Server, sin abrir
conexiones: las ocho proyecciones reales de resúmenes/catálogos, y la unión de
movimientos extraída del método para el verificador temporal, con filtros y
paginación. Se generó UNION ALL con OFFSET/FETCH, sin perder relaciones eliminadas
ni seleccionar PasswordHash. Esta comprobación de traducción no sustituye una
prueba funcional de consultas, tipos o concurrencia en SQL Server.

La solución compiló con 0 errores y 0 advertencias. tools/Schema confirmó once
entidades, catorce migraciones y restricciones conservadas; dotnet-ef 9.0.11 confirmó
que no hay cambios pendientes de modelo. No se crearon/aplicaron migraciones, no
se conectó a bases personales y no se incorporaron pruebas al repositorio.
Se reutilizó la adaptación temporal de descuentos SQLite de los bloques anteriores,
sin modificar la representación decimal del modelo de producción ni ejecutar de
nuevo sus suites completas. La validación integral de SQL Server queda para 12.3.

### 14.3 Validación integral de SQL Server — bloque 12.3

Se partió del main `dc37184` con el PR #14 integrado. El usuario autorizó continuar
con el bloque 12.3 y reiteró que el carrito solo puede solicitar entre cero y el
stock disponible. Cero se interpreta como retirar la línea, sin persistir un detalle
cero ni mover stock. Se explicaron los archivos afectados y el cambio de respuesta
204 antes de implementar. La confirmación sigue permitiendo stock final cero.

Se utilizaron .NET SDK 9.0.320, dotnet-ef 9.0.11 y SQL Server 2022 Developer en Linux,
con imagen verificada por digest
`sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718`.
El servidor y la API escucharon solo en loopback. Credenciales y claves se generaron
para la prueba, fuera de Git. Las imágenes se guardaron en un webroot externo al
checkout. Todos los scripts y resultados temporales quedaron fuera del repositorio.

La primera ejecución detectó dos problemas: el carrito admitía más que el stock,
y una carrera de salidas devolvió 500 porque EF Core envolvía un deadlock 1205 en
InvalidOperationException. Se corrigieron SalidaDtos, SalidasController y
SalidaService para altas/cambios/cero y revalidación al solicitar. Se agregó
ConflictosSql, compartido por IngresoService y SalidaService, para reconocer solo
1205/1222 en la cadena de excepciones y responder 409 sin reintento automático.
No se alteraron entidades, restricciones, snapshot ni migraciones.

Durante la preparación también se corrigió el script temporal de inyección de
fallos: CREATE TRIGGER necesita iniciar su batch y el comando debía apuntar a la
misma base que la API. Esa falla del harness no se presentó como defecto de la API.
La ejecución consolidada final se hizo desde otra base vacía con ambas correcciones.

**Resultado funcional:** 454 comprobaciones aprobadas, cero fallidas, doce escenarios:

1. JWT, roles, rutas anónimas, firma inválida, credenciales incorrectas y CORS.
2. CRUD, validación, email/CUIT normalizado único, paginado y revocación por baja.
3. Ingreso borrador/confirmado, precio/descuento, bloqueo de detalles y reversión.
4. Carrito, detalle único, IDOR, solicitud y confirmación por ambos operadores.
5. Falta de stock y reversión de ingreso rechazada sin cambios parciales.
6. Cantidad 0..stock: cero retira, exceso no crea ni modifica y solicitud revalida.
7. Rollback de ingreso y salida ante error SQL real en el segundo producto.
8. Tres rondas de doble confirmación de ingresos, doble confirmación de salidas
   y ventas distintas compitiendo por stock: una operación 200 y la otra 409.
9. Auditoría, filtros, permisos y producto eliminado.
10. Carga/reemplazo real de imágenes PNG en Linux, MIME, archivo corrupto,
    autorización, limpieza del archivo anterior y cabecera nosniff.
11. Rollback del soft delete de un ingreso si falla la reversión del segundo
    producto, seguido de un reintento que resta una sola vez.
12. Históricos con producto, categoría, proveedor, cliente y empleado eliminados:
    se conservan contrapartes y confirmadores; los tokens dados de baja se rechazan.

Las consultas finales comprobaron cero stocks negativos, cero detalles con cantidad
no positiva y cero duplicados de Producto por Salida. Los fallos SQL inyectados
respondieron 500 de forma deliberada y revirtieron todo; no se confundieron con los
conflictos recuperables 409.

**Migraciones:** las catorce se aplicaron desde cero en bases de prueba. Además se
hicieron siete comprobaciones de actualización: base detenida en migración once,
aplicación de las tres restantes, preservación del confirmador y estado de una
salida existente, normalización calculada de CUIL de cliente y CUIT de proveedor,
y repetición de database update sin reaplicar migraciones. No se probó convertir
el historial legacy ni hacer downgrade con confirmaciones de Administrador.

La solución compiló sin errores ni advertencias y EF Core no detecta cambios de
modelo pendientes. Las bases descartables se eliminaron al detener el contenedor
creado para este bloque. No se aplicaron migraciones a una base personal. La suite
retirada no se reintrodujo ni se afirma haber reejecutado todas sus pruebas históricas.

**Entrega:** el usuario autorizó registrar y subir los cambios de este bloque en
una rama para su revisión previa al merge. No se autoriza el merge automático. Para instalar: copia de seguridad si existe una base, revisar su historial,
configurar conexión/JWT de forma segura, usar EF Core 9.0.11 y aplicar únicamente la
cadena compatible; después ejecutar el bootstrap solo si no existe Administrador.
No usar el modo bootstrap ni database update automáticamente al arrancar la API.
El carrito no reserva stock: una venta ajena puede invalidar un carrito existente,
por lo que solicitar y confirmar deben volver a comprobar disponibilidad.

### 14.4 Protección del último Administrador — mejoras, bloque 1

Alcance autorizado: UsuariosController, AuthService y esta documentación. La
eliminación se ejecuta dentro de una transacción Serializable y considera únicamente
Administradores activos con perfil consistente como respaldo. No agrega una
prohibición general de autoeliminación. No cambia CrearAdministradorInicialAsync:
sigue contando administradores eliminados al impedir un segundo bootstrap.

Se ejecutaron 186 comprobaciones temporales fuera del repositorio, con JWT de login
real, una API local y una base SQL Server 2022 descartable. Todas aprobaron, sin
omisiones. Se verificó:

- Un único Administrador no puede eliminarse (409), conserva cuenta y token.
- Otro Administrador puede eliminarse o autoeliminarse cuando queda uno activo.
- Empleados/Clientes y administradores eliminados o con perfil inconsistente no
  cuentan como respaldo utilizable.
- Los roles no administrativos no pueden usar DELETE de Usuarios y los anónimos
  reciben 401. El borrado de Empleados/Clientes conserva perfiles y revoca tokens.
- Repetir DELETE o editar un objetivo eliminado devuelve 404; no existe borrado físico.
- Seis rondas de autoeliminaciones simultáneas, seis de eliminaciones cruzadas y
  tres contra un mismo objetivo. En cada carrera ganó una operación con 204 y la
  otra respondió 409; nunca quedó el sistema sin Administrador activo.
- La auditoría conserva los administradores eliminados. Las catorce migraciones
  existentes permanecen sin cambios.

Compilación sin errores ni advertencias. EF Core comprobó que el modelo y snapshot
siguen coincidiendo. Se aplicó el esquema únicamente a la base descartable para
probar el código; no se creó una migración nueva ni se tocó una base personal.
La instancia descartable y la API de prueba se detuvieron al finalizar. La suite
versionada retirada no se reintrodujo ni se reejecutó toda la batería de 12.3.

## 15 Plan de trabajo y alcance por autorizar
Plan de mejoras acordado después del informe de revisión:

| Bloque | Alcance | Estado |
| --- | --- | --- |
| 1 | Proteger al último Administrador activo | Implementado y validado; entrega por rama autorizada para revisión |
| 2 | Limitar intentos de login | Pendiente de alcance y autorización |
| 3 | Acordar una batería pequeña de pruebas reproducibles | Pendiente; no reintroducir pruebas versionadas por suposición |
| 4 | Simplificar paginado, normalización y grupos de permisos | Pendiente, conservando contratos y matriz de autorización |
| Resto | Otras recomendaciones del informe | Postergadas hasta finalizar los cuatro bloques |


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
| 9.5 | CRUD de productos e imágenes | Implementado e integrado en PR #10 |
| 10 | Flujo completo de ingresos y stock | Integrado en PR #11 |
| 11 | Flujo completo de salidas y stock | Integrado en PR #12 |
| 12 | Validación integral y entrega | Completado e integrado en PR #15 |
| 12.1 | Completar y corregir Usuarios | Integrado en PR #13 |
| 12.2 | Consultas administrativas de históricos y eliminados | Integrado en PR #14 |
| 12.3 | Validación integral en SQL Server y preparación de entrega | Integrado en PR #15 |

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

El usuario autorizó el paso 10 con el alcance de la sección 11.5, incluida captura
del precio desde Producto y redondeo AwayFromZero por detalle. Se modifican
IngresosController, IngresoDtos y el registro de dependencias; se agrega
IngresoService y se retira únicamente el guard de ingresos de MovimientoService.
Se actualiza este mismo documento. La autorización cubre cambios locales y
verificaciones temporales. Posteriormente autorizó el commit y la subida de esta
entrega para revisión mediante PR. El merge queda a su cargo y no se autoriza
aplicar migraciones.

El usuario confirmó explícitamente que tanto Administrador como Empleado deben
confirmar salidas y autorizó el alcance concreto del paso 11: carrito propio,
detalle único, precio capturado y descuento inicial cero, solicitud que bloquea
detalles, descuento atómico al confirmar y eliminación previa a confirmación
operativa. Autorizó sustituir EmpleadoId por UsuarioConfirmacionId con FK a Usuario,
generar la migración correspondiente y actualizar esta documentación. Se modifican
Salida, su configuración, la navegación inversa de Empleado, DTOs, controlador,
registro de dependencias y herramientas de inspección; se reemplaza MovimientoService
por SalidaService. Se permiten verificaciones temporales externas al repositorio.
Posteriormente el usuario autorizó el commit y la subida de esta entrega a una
rama del proyecto para revisión mediante PR. El merge queda a su cargo y no se
autoriza aplicar la migración.

El usuario autorizó el bloque 12.1 después de la revisión estática del proyecto:
completar POST de Usuarios reutilizando AuthService, validar y normalizar los DTO,
rechazar campos desconocidos, controlar emails duplicados incluyendo eliminados,
validar paginación y preservar rol, perfiles, CUIL de clientes, cambio opcional de
contraseña y soft delete. Se modifican UsuariosController, UsuarioDtos y esta
misma documentación. La autorización inicial cubrió cambios locales y verificaciones
temporales externas al repositorio. Posteriormente el usuario autorizó el commit,
la subida de la rama y la entrega para revisión mediante PR. El merge queda a su
cargo; no se autorizan migraciones ni cambios en bases personales. Auditoría e históricos y validación integral son bloques
posteriores que requieren su propio alcance aprobado.

El usuario autorizó el bloque 12.2 después de revisar su alcance: rutas GET
exclusivas de Administrador para listados/detalles de los ocho recursos, filtros
de estado y referencias/fechas, detalles e importes de ingresos/salidas, movimientos
de productos eliminados e imagen actualmente asociada. Se incluyen todas las
relaciones conservadas sin perder registros por filtros globales. Se explicó el
límite del modelo respecto de fecha/autor de borrado y versiones anteriores.
La autorización cubre implementación local de AuditoriaController, AuditoriaDtos,
AuditoriaService, registro en Program y este documento, con comprobaciones temporales
fuera del repositorio. Posteriormente el usuario autorizó el commit, la subida
de la rama y la entrega para revisión mediante PR. El merge queda a su cargo; no
se autoriza aplicar migraciones ni modificar bases personales. No se introducen
escrituras de auditoría.

## 16 Historial de entregas

Estado verificado al iniciar el bloque 12.3: PR #1 al #14 integrados.
El usuario autorizó la entrega del bloque 12.3 en `validacion/paso-12-3`
para revisión previa al merge; comprobar el estado actual en GitHub.
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
| Productos e imágenes y retiro de pruebas | `codex/paso-9-5-productos-imagenes` | `9929183` y `c5f20cc` | [#10](https://github.com/LuAlzuri/TPN-5-Movil-Correcion/pull/10) |
| Ingresos y stock | `codex/paso-10-ingresos-stock` | `6f4150e` y `016d5db` | [#11 integrado](https://github.com/LuAlzuri/TPN-5-Movil-Correcion/pull/11) |
| Salidas, carrito y stock | `codex/paso-11-salidas-stock` | `e078981` inicial | [#12 integrado](https://github.com/LuAlzuri/TPN-5-Movil-Correcion/pull/12) |
| Usuarios | `codex/paso-12-1-usuarios` | `2783113` inicial | [#13 integrado](https://github.com/LuAlzuri/TPN-5-Movil-Correcion/pull/13) |
| Consultas de auditoría e históricos | `codex/paso-12-2-auditoria` | `20ee235` inicial | [#14 abierto](https://github.com/LuAlzuri/TPN-5-Movil-Correcion/pull/14) |

El PR #10 ya está integrado mediante `ec5f3ef`, comprobado al iniciar el paso 10.
El paso 10 se publicó en `codex/paso-10-ingresos-stock`, basada en `origin/main`
con ese merge, mediante el commit inicial `6f4150e`. El usuario integró el PR #11 mediante `6e35de1`, verificado antes de crear la rama
local `codex/paso-11-salidas-stock`. El paso 11 se publicó con el commit inicial
`e078981` mediante el PR #12. Esta actualización documental se publica en la misma
rama como seguimiento de la entrega. El usuario integró el PR #12 mediante
`2775d66`, verificado al iniciar el bloque 12.1. La rama local de ese bloque parte
de ese main actualizado. El bloque 12.1 se publicó con el commit inicial `2783113`
mediante el PR #13. Esta actualización documental se publica como seguimiento en
la misma rama. El usuario integró el PR #13 mediante `7269ef6`, comprobado al
iniciar el bloque 12.2. Este bloque parte de ese main actualizado y se publicó
con el commit inicial `20ee235` mediante el PR #14. Esta actualización documental
se publica como seguimiento en la misma rama. El merge queda a cargo del usuario.

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
| Crear ingreso/salida altera stock inmediatamente | Pasos 10 y 11: stock cambia solo al confirmar operativamente |
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
