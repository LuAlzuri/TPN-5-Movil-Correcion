# Paso 8: autenticación, autorización y perfiles

## Registro de cuentas

POST /api/auth/register requiere un JWT de Administrador. No existe autorregistro
público ni un rol elegido libremente por una cuenta Cliente o Empleado.

```json
{
  "nombre": "Ana",
  "apellido": "Ejemplo",
  "cuit": "20-12345678-9",
  "email": "ana@example.com",
  "password": "REEMPLAZAR-POR-UNA-CONTRASEÑA-PROPIA",
  "rol": "Empleado",
  "fechaIngreso": "2026-10-26"
}
```

No usar la contraseña ilustrativa. La contraseña real requiere entre 12 y 128
caracteres; se almacena mediante PasswordHasher, nunca como texto plano. La política
también se aplica al cambio de contraseña de la ruta de usuarios existente.

- Administrador: sin subtabla ni datos específicos de otro perfil.
- Empleado: FechaIngreso obligatoria; sin Telefono ni Direccion de Cliente.
- Cliente: Telefono/Direccion opcionales; sin FechaIngreso.

La cuenta y su único perfil se insertan en una transacción. El ID del perfil es el ID
de Usuario; el request no recibe IDs de cuentas ni perfiles. El servicio rechaza roles
inválidos, perfiles mezclados y emails duplicados, incluyendo cuentas eliminadas.
Los emails se guardan en minúsculas; el login busca sin distinguir mayúsculas. Los
índices existentes protegen la unicidad ante registros simultáneos en SQL Server.
La respuesta 201 apunta a GET /api/usuarios/{id} y no incluye el hash ni contraseña.

POST /api/usuarios y POST /api/clientes siguen temporalmente en 501: se completarán
con sus CRUD del paso 9. Ya se pueden crear las tres clases de cuenta mediante el
registro autenticado. Este bloque no implementa los flujos de inventario pendientes.

## JWT y cuenta vigente

POST /api/auth/login valida email, contraseña y perfil; responde 401 con un mensaje
genérico ante credenciales inválidas, cuenta eliminada o perfil inconsistente.
GET /api/auth/me devuelve la cuenta autenticada.

El token usa HS256, emisor/audiencia configurados y claims sub, name, email, role,
auth_version y jti. La expiración de la respuesta coincide exactamente con exp.
No contiene contraseña ni PasswordHash; auth_version es un digest del hash que
permite invalidar tokens cuando cambia la contraseña, sin agregar columnas al modelo.

Cada petición autenticada consulta la cuenta y sus perfiles: rechaza cuentas
eliminadas, rol cambiado, perfiles faltantes o dobles, y contraseña cambiada. Esto
implica una consulta adicional y dependencia de la base en las rutas autenticadas.
Si la base falla no se permite el acceso por omisión. Los tokens de etapas anteriores
no son compatibles: se debe iniciar sesión nuevamente.

UsuarioAutenticado.ObtenerUsuarioId/ObtenerClienteId/ObtenerEmpleadoId permiten a los
futuros controladores obtener el ID del principal validado. Nunca obtener la identidad
del cliente o empleado desde el JSON para las operaciones de inventario.

Se valida firma, HS256, emisor, audiencia y vencimiento con tolerancia de reloj de 30
segundos. No se agrega refresh token. CORS continúa permisivo, sin credenciales de cookies.

## Configuración y primer administrador

appsettings.example.json es una plantilla sin secretos. Proveer DefaultConnection,
Jwt:Issuer, Jwt:Audience y Jwt:Key mediante configuración local ignorada o variables
de entorno. La clave JWT debe tener al menos 32 bytes; no hay valor predeterminado.
Jwt:ExpireMinutes acepta 1 a 1440, con 120 como duración predeterminada.

Para una base NUEVA y vacía, revisar primero el destino y aplicar las once migraciones
existentes con dotnet-ef 9.0.11. No ejecutar esta cadena sobre el historial anterior.
Desde la raíz, con la configuración JWT y conexión ya definidas:

```bash
dotnet ef database update --project 'TPN°5-Movil/TPN°5-Movil.csproj'
```

Después, definir Bootstrap__Nombre, Bootstrap__Apellido, Bootstrap__CUIT y
Bootstrap__Email. Ingresar Bootstrap__Password de forma segura, por ejemplo mediante
un prompt en Git Bash para no escribirla en el historial:

```bash
read -r -s -p 'Contraseña inicial: ' Bootstrap__Password
export Bootstrap__Password
dotnet run --project 'TPN°5-Movil/TPN°5-Movil.csproj' --no-launch-profile -- --crear-administrador
unset Bootstrap__Password
```

No se imprime la contraseña ni se inicia HTTP en ese modo. El comando requiere el
esquema ya preparado, no aplica migraciones automáticamente y rechaza otra creación
si ya existe un Administrador, aunque esté eliminado. El bloqueo se realiza dentro
de una transacción Serializable. No agregar secretos a Git ni al PR.

## Pruebas

Desde la raíz, usando .NET 9:

```bash
dotnet test tests/Auth.Integration/Auth.Integration.csproj
```

Por defecto usa SQLite en memoria, con una base distinta por caso. No necesita SQL
Server ni credenciales reales. WebApplicationFactory ejecuta la canalización HTTP,
el servicio real y las transacciones; las claves y contraseñas del fixture son de
prueba y no se usan al iniciar la aplicación normalmente.

Para probar el proveedor real, definir AUTH_TEST_SQLSERVER_CONNECTION apuntando
EXCLUSIVAMENTE a una instancia de pruebas descartable. Cada fixture crea una base
TPN5_AuthTest_<GUID> y ejecuta las migraciones SQL Server reales; no usa el nombre de
base del template. Requiere permiso para crear bases. Las bases permanecen en esa
instancia hasta descartarla: nunca usar un servidor de producción para esta suite.
La prueba de rollback agrega una restricción temporal únicamente a su base aislada.

La prueba histórica tools/smoke_step7.py usaba tokens artificiales sin cuenta; ya no
es válida para las rutas autenticadas. Usar la suite de integración de este bloque.

No se modifican entidades ni esquema; no corresponde generar una migración vacía.

## Resultados de esta entrega

- Compilación sin errores ni advertencias.
- 25 pruebas aprobadas en SQLite y las mismas 25 aprobadas en SQL Server 2022;
  ninguna omitida. Incluyen permisos, perfiles, rollback ante falla de inserción,
  credenciales incorrectas, email eliminado, revocación por contraseña o soft delete,
  rechazo de firma/audiencia/expiración inválidas y CORS.
- Las once migraciones existentes se aplicaron en bases SQL Server nuevas de prueba.
- El comando CLI se probó sobre otra base nueva: creó el Administrador sin iniciar
  HTTP y rechazó una segunda ejecución.
- El modelo no cambió y EF Core no detecta cambios pendientes de migración.
- No se modificó ninguna base del usuario. La instancia descartable de pruebas se
  detuvo al finalizar; los resultados anteriores no significan que tu base ya esté
  configurada o migrada.
