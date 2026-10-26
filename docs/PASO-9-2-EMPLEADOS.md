# Paso 9.2 Empleados

El CRUD de `/api/empleados` requiere Administrador en todas las operaciones.
La respuesta contiene Id, Nombre, Apellido, CUIT, Email y FechaIngreso. No incluye
contraseñas ni hashes. El ID corresponde tanto a Usuario como a Empleado.

## Rutas

- `GET /api/empleados`: empleados activos con búsqueda y paginado.
- `GET /api/empleados/{id}`: consulta individual.
- `POST /api/empleados`: crea Usuario y Empleado mediante AuthService.RegisterAsync,
  reutilizando su transacción explícita. Devuelve 201 y Location de la consulta individual.
- `PUT /api/empleados/{id}`: modifica datos personales y FechaIngreso. Los cambios
  de Usuario y Empleado se guardan juntos mediante la transacción de SaveChanges.
- `DELETE /api/empleados/{id}`: marca Usuario.Eliminado. Conserva el perfil y
  todas las referencias históricas. Los tokens existentes dejan de ser válidos
  mediante la comprobación por petición ya implementada en autenticación.

Las consultas y modificaciones se limitan a perfiles activos con rol Empleado
y sin perfil Cliente. Los IDs inexistentes, eliminados o de otro tipo de cuenta
devuelven 404. No se incorporan rutas de restauración ni auditoría.

## Entrada y validación

Crear requiere Nombre y Apellido (hasta 150 caracteres), CUIT (hasta 20), Email
válido (hasta 150), Password (12 a 128) y FechaIngreso. Editar requiere los mismos
campos salvo Password. La fecha no puede omitirse ni ser 0001-01-01; no se
impone una restricción adicional de fecha pasada porque no fue acordada.

Los campos de texto se guardan sin espacios exteriores y el email en minúsculas.
El email no se puede repetir, incluyendo cuentas eliminadas y otros roles, y la
búsqueda de duplicados no distingue mayúsculas. Los conflictos devuelven 409;
también se interceptan las colisiones de índices únicos de SQL Server al editar.
No se agrega unicidad ni validación de formato de CUIT que el modelo no establezca.

El rol se fija como Empleado en el servidor. Los DTOs rechazan propiedades JSON
desconocidas, incluyendo Rol, Id o datos de Cliente. La edición rechaza Password;
el cambio de contraseña continúa en la ruta existente de usuarios.

El listado acepta page positivo y pageSize de 1 a 100 (valores por defecto 1 y 10).
Rechaza con 400 parámetros inválidos y desplazamientos fuera de Int32. Ordena por
Apellido, Nombre e Id. La búsqueda elimina espacios exteriores y consulta nombre
completo, email o CUIT. Una página fuera del resultado devuelve Items vacío.

## Alcance y verificación

Se agregan EmpleadosController, EmpleadoDtos, pruebas de integración y este
documento. No cambian entidades, DbContext ni migraciones. No se modifica el CRUD
general de usuarios ni el de clientes.

Ejecutar `dotnet test tests/Auth.Integration/Auth.Integration.csproj` con
AUTH_TEST_SQLSERVER_CONNECTION sin configurar para usar SQLite aislada.
Las pruebas incluyen permisos, validaciones, perfiles compartidos, rollback al
crear y editar, duplicados, paginado, búsqueda, preservación del histórico y
revocación tras borrado lógico. La prueba existente compara el modelo SQL Server
con las migraciones sin abrir conexión.

Resultado local: 54 pruebas aprobadas, 0 fallidas y 0 omitidas, incluyendo las
pruebas previas de autenticación y categorías. Compilación sin advertencias.
Las pruebas funcionales se ejecutaron en SQLite aislada; no se ejecutaron contra
SQL Server ni se aplicaron migraciones a bases personales.
