# Paso 7: integración y compilación

## Cambios

La API vuelve a compilar con el modelo de encabezados, detalles y perfiles. No se
agregan ni aplican migraciones: el esquema no cambió. El snapshot existente se
reubica desde tools/Schema/TPN_5_MOVIL/Migrations a TPN°5-Movil/Migrations para que
la API principal también pueda descubrirlo al generar futuras migraciones.

Los DTOs de usuario y cliente separan nombre y apellido, incluyen CUIT y leen datos
personales desde Usuario. UsuarioDto expone Eliminado en lugar de Activo. La edición
común de usuario no admite cambiar el rol, restaurar cuentas ni suprimir su perfil.

IngresoResponse y SalidaResponse exponen encabezado, estados, fechas y lista de detalles.
El filtro de producto recorre esos detalles. IngresoRequest recibe proveedor y
observaciones; SalidaRequest recibe observaciones, sin ClienteId ni EmpleadoId.
Los totales y las validaciones del flujo completo corresponden a los bloques 10 y 11.

Los contratos de producto ya no reciben ni devuelven proveedor directo; ImagenUrl
se obtiene de Imagen.RutaRelativa. El filtro proveedorId consulta detalles de ingreso.
La consulta de movimientos lee únicamente movimientos confirmados y está restringida
a Administrador y Empleado porque incluye contrapartes. La consulta histórica completa
con registros eliminados y permisos de auditoría sigue pendiente.

Se reemplazan los nombres de rol anteriores por Administrador, Empleado y Cliente.
El login se adapta a los campos nuevos; esto NO completa la autenticación del paso 8.
Los borrados de usuarios, clientes, categorías, proveedores y productos pasan a marcar
Eliminado sin quitar filas ni archivos. Los controles de duplicados de claves únicas
incluyen registros eliminados para no ofrecer reutilizaciones que la base rechazaría.
La política completa y validación de los CRUD se completará en su bloque correspondiente.

## Operaciones temporalmente no disponibles

Responden 501, con un mensaje explícito, después de los controles de autorización:

- POST /api/auth/register
- POST /api/usuarios
- POST /api/clientes
- POST /api/ingresos
- POST /api/salidas
- POST /api/productos/{id}/imagen (con archivo válido en multipart/form-data)

No se conserva el flujo anterior que modificaba stock al crear un movimiento.
No se inventan IDs de empleados, clientes o cuentas para satisfacer nuevas FK.
No se crean cuentas sin su perfil ni imágenes sin metadatos.
Las consultas de salidas están temporalmente limitadas a Administrador/Empleado:
las consultas del cliente requieren implementar primero el alcance por identidad.
Estos límites son intermedios y deberán reemplazarse al completar los pasos 8 a 11.

DbInitializer sigue sin ejecutarse al iniciar la API. Se adapta únicamente el catálogo
de ejemplo: productos con stock cero y sin proveedor directo. Se retira la creación de
clientes independientes y el administrador con contraseña fija. Ya no aplica migraciones
automáticamente. Su ejecución manual requiere un esquema previamente preparado; no se
probó ni ejecutó el seed en este bloque.

## Validación

Desde la raíz, con .NET SDK 9 y dotnet-ef 9.0.11:

```bash
dotnet build 'TPN°5-Movil/TPN°5-Movil.csproj'
dotnet run --project tools/Schema/Schema.csproj
dotnet ef migrations has-pending-model-changes --project tools/Schema/Schema.csproj
```

Con la configuración JWT local de desarrollo, también se verificó el snapshot desde
el proyecto principal sin conectarse ni ejecutar cambios en la base:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet ef migrations has-pending-model-changes --project 'TPN°5-Movil/TPN°5-Movil.csproj'
```

Para la prueba HTTP, iniciar desde TPN°5-Movil:

```bash
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:5087 dotnet run --no-build --no-launch-profile
```

En otra terminal, desde la raíz:

```bash
python tools/smoke_step7.py
```

El script usa únicamente la biblioteca estándar de Python y configuración JWT local
(no imprime claves ni tokens). Firma tokens de prueba para ejercitar rutas: NO prueba
el login real ni la creación de cuentas. Los tokens de prueba caducan a los dos minutos.

Resultados: build sin errores ni advertencias; 12 comprobaciones del modelo; snapshot
sin cambios pendientes en ambos proyectos; 15 comprobaciones HTTP, contratos Swagger
y CORS permisivo. Las operaciones con base de datos y archivos reales siguen pendientes
de sus bloques funcionales y de una instancia SQL Server disponible. No se ejecutaron
migraciones, seeds, confirmaciones, ventas ni pruebas de concurrencia.
