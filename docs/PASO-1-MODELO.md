# Paso 1: modelo y migraciones

Este cambio prepara exclusivamente las entidades, el DbContext y un historial nuevo
para una base SQL Server vacía. No aplica migraciones ni implementa todavía los nuevos
controladores, DTOs, autenticación, stock transaccional o servicios.

## Modelo

Usuario tiene un solo rol: Administrador, Empleado o Cliente. Empleado.Id y Cliente.Id
son PK/FK hacia Usuario.Id. La exclusividad de perfiles y su correspondencia con el rol
necesitan validación transaccional en los servicios futuros; las FK no la garantizan.
Eliminado reside en Usuario y los perfiles filtran por ese estado.

Ingresos y salidas se separan en encabezados y detalles. El ingreso guarda el empleado
creador y, al confirmarse, el empleado confirmador y la fecha. La salida admite empleado
nulo hasta su confirmación. No existen atributos RowVersion.

Las restricciones SQL protegen stock >= 0, cantidades >= 1, precios >= 0 y descuentos
entre 0 y 100. DetalleSalida impide duplicar un producto por encabezado. Imagen tiene
ProductoId único, tamaño en bytes y metadatos; la carga de archivos se implementará luego.
Los precios históricos son los guardados en los detalles. Los subtotales y totales se
calcularán en los siguientes servicios/DTOs, con descuento porcentual y redondeo uniforme.

Todas las FK tienen Restrict. Los filtros globales ocultan registros eliminados; los
históricos requerirán consultas explícitas con IgnoreQueryFilters, incluyendo controles
de autorización. Cargar navegaciones requeridas hacia registros filtrados puede ocultar
filas: no debe usarse como consulta de auditoría.

Las futuras operaciones de confirmación deben usar transacciones explícitas y cambios
condicionados del estado y stock, comprobando filas afectadas. Este modelo por sí solo
no impide confirmaciones concurrentes ni ediciones después de confirmar.

## Migraciones

Las migraciones anteriores se conservaron textualmente en migrations-legacy/*.cs.txt
como referencia fuera de la compilación. Las once nuevas migraciones crean una entidad
cada una, ordenadas por dependencia, con sus snapshots de destino generados por EF Core.

NO aplicar esta cadena a una base creada con la migración Inicial anterior, aunque se use
un script idempotente. No incluye conversión de datos ni reinicio automático de bases.
No ejecutar database drop ni database update sin revisar y autorizar el destino.

## Verificación independiente del esquema

Requiere SDK .NET 9 y dotnet-ef 9.0.11. Desde la raíz del repositorio:

```bash
dotnet run --project tools/Schema/Schema.csproj
dotnet ef migrations has-pending-model-changes --project tools/Schema/Schema.csproj
dotnet ef migrations script --project tools/Schema/Schema.csproj --idempotent --output esquema.sql
```

El proyecto auxiliar enlaza las entidades, contexto y migraciones reales; no los duplica.
Su factory de diseño no necesita conectarse para generar SQL. Para cualquier futura
operación sobre una base se debe definir ConnectionStrings__DefaultConnection de forma
segura. No usar el nombre TPN5_DesignOnly de la conexión de scaffolding como destino real.
El archivo dotnet-tools.json anterior no fija una versión compatible de EF; se utilizó
una instalación externa de dotnet-ef 9.0.11 sin modificar dependencias de la API.

## Alcance pendiente

La API completa está en transición: sus DTOs, controladores, AuthService,
MovimientoService y DbInitializer siguen usando miembros del modelo anterior. Por eso
su compilación todavía falla. No desplegar ni ejecutar este bloque como una API lista.
Estas adaptaciones corresponden a los siguientes bloques de autorización.

No se validó la ejecución del SQL en SQL Server. La descarga de su imagen estaba
bloqueada por la red del entorno. Generar SQL y comprobar el modelo no sustituye esa prueba.

## Actualización posterior

El paso 7 recupera la compilación y documenta las operaciones temporalmente no
disponibles. Consultar PASO-7-INTEGRACION.md para el estado actual; el estado de
compilación descrito arriba corresponde exclusivamente al cierre del paso 1.
