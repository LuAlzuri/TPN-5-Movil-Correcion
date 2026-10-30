# Integración de pantallas del Plan 1

Implementación en una app MAUI Android, dentro de Apinventario.slnx junto al backend. El servidor mantiene reglas y autorización. No se crearon endpoints, tablas, migraciones ni seeds para implementar estas pantallas. El script de semilla previo sigue siendo ajeno a esta entrega.

## Conexiones

Prefijo de rutas HTTP `/api`. Las páginas administrativas requieren rol Administrador y están protegidas con AuthorizeRouteView. Las consultas no generan confirmaciones; baja de usuario/categoría y envío de correo requieren confirmación explícita. Guardados bloquean doble envío mientras están en curso.

| Pantalla | Solicitud | Datos y resultado |
|---|---|---|
| /login | POST /auth/login | `{email,password}`; recibe token, expiresAt, usuario. Sin selector de rol. |
| Restauración de sesión | GET /auth/me | Bearer de SecureStorage; valida cuenta/rol antes de restaurar navegación. |
| Cerrar sesión | Local | Borra token, usuario y estado autenticado; no hay endpoint de logout. |
| /inventario | Sin consulta | Inicio Personal con enlaces permitidos. Comercio se conecta con Plan 2. |
| /admin/usuarios | GET /usuarios | page, pageSize=20, busqueda; PageResult<UserInfo>. Cuentas activas. |
| /admin/usuarios/nuevo | POST /auth/register | Nombre, apellido, CUIT, email, password y rol. Empleado fechaIngreso; Cliente telefono/direccion; Administrador sin perfil. |
| /admin/usuarios/{id} | GET /usuarios/{id} | Recupera cuenta y rol de solo lectura. |
| Guardar datos generales | PUT /usuarios/{id} | Nombre, apellido, CUIT, email y password solo si se cambió. Nunca envía rol. Espera 204. Si es la cuenta propia cierra sesión para revalidarla. |
| Perfil empleado | GET/PUT /empleados/{id} | Datos generales y fechaIngreso DateOnly. Guardado separado del cambio de contraseña. |
| Perfil cliente | GET/PUT /clientes/{id} | Datos generales y telefono/direccion. Guardado separado. |
| Dar de baja cuenta | DELETE /usuarios/{id} | Confirmación y 204; muestra 409 si es último administrador o hay conflicto. |
| /admin/categorias | GET /categorias | page, pageSize=20, busqueda; devuelve paginación y fechaCreacion. |
| Formulario categoría | GET /categorias/{id}; POST /categorias; PUT /categorias/{id} | Nombre requerido <=100 y descripción <=1000. POST devuelve entidad y PUT espera 204. |
| Baja categoría | DELETE /categorias/{id} | Borrado lógico confirmado; no afirma cascada de productos. |
| /admin/auditoria, Acciones | GET /auditoria/acciones | page, pageSize=20, usuarioId, entidad, accion, desde, hasta. DTO `{items,total,page,pageSize}`. |
| /admin/auditoria/acciones/{id} | GET /auditoria/acciones/{id} | Id long; actor/email histórico/IP/fecha y referencia del origen. 404 si no existe. |
| Historial de entidades | GET /auditoria/usuarios, /productos, /ingresos, /salidas | PageResult<JsonElement> con filtros específicos. Visualización histórica, incluyendo bajas y referencias eliminadas. |
| /admin/auditoria/historial/{resource}/{id} | GET /auditoria/{resource}/{id} | Resource validado en lista permitida; detalles anidados, actores, líneas e importes. |
| Historial de producto, movimientos | GET /auditoria/productos/{id}/movimientos | page, pageSize, tipo, desde y hasta. Cantidad e impacto vigente separados. |
| /admin/reportes, consultar | GET /reportes/stock, /ingresos, /salidas | Movimientos admiten desde/hasta DateOnly. Verifica PDF y lo abre/compartir desde caché privada. |
| /admin/reportes, enviar | POST /reportes/{tipo}/enviar | `{destinatario,desde,hasta}`; destinatario explícito y confirmación de correo real; éxito solo con enviado=true. |

Ejemplo de alta ficticia de empleado (sin credenciales reales):

```json
{"nombre":"Empleado","apellido":"Ensayo","cuit":"dato-de-ensayo","email":"empleado@example.test","password":"CLAVE_DE_ENSAYO_DE_12_O_MAS","rol":"Empleado","fechaIngreso":"2026-10-29","telefono":null,"direccion":null}
```

Ejemplo de respuesta de acciones: `{"items":[{"id":123,"usuarioId":2,"usuarioEmail":"empleado@example.test","accion":"Confirmar","entidad":"Ingreso","entidadId":"8","fechaUtc":"2026-10-29T12:00:00","ip":null,"observacion":null}],"total":1,"page":1,"pageSize":20}`. El id de evento no es el id del ingreso. No hay editar/eliminar acciones.

## Filtros y fechas

- Usuarios históricos: eliminado, busqueda y rol.
- Productos históricos: eliminado, busqueda y categoriaId.
- Ingresos históricos: eliminado, productoId, proveedorId, confirmado, desde/hasta.
- Salidas históricas: eliminado, productoId, clienteId, confirmarXCliente, confirmarXEmpleado, desde/hasta.
- Acciones: filtros de igualdad entidad/accion y usuarioId; no busqueda ni eliminado.
- Movimientos: tipo Ingreso/Salida y desde/hasta.

Las pantallas de auditoría seleccionan días de negocio UTC−03 y convierten los límites inclusivos a UTC. Los reportes envían DateOnly sin doble conversión porque el servidor aplica ese criterio. Fechas históricas SQL sin sufijo se interpretan como UTC; fechas de ingreso de empleado se conservan como DateOnly.

## Errores

ApiFailure conserva validaciones por campo, aunque los formularios actuales muestran un resumen. 401 de solicitud autenticada limpia sesión; 401 de login muestra error; 403 no destruye sesión válida; 404 ofrece volver/reintentar; 409 pide revisar estado sin repetir escritura; 413 indica archivo grande; 429 expone RetryAfter y login tiene cuenta regresiva; 5xx oculta detalles internos; red/timeout registran etiqueta sanitizada. Si una escritura quedó con resultado incierto, consultar antes de repetir. Los servicios no reintentan POST/PUT/DELETE.

Reportes mayores que 1000 filas/cabeceras o 2000 detalles deben acotarse; no se anuncia PDF parcial. La descarga no sustituye comprobar correo. La pantalla de correo está implementada, pero la configuración local inspeccionada tiene SMTP deshabilitado y sin host. No se guardó el destinatario personal proporcionado por el usuario en código/Git. Prueba de recepción real pendiente de configurar SMTP seguro y habilitarlo en servidor.

## Auditoría y logging

La bitácora actual registra Confirmar/Ingreso, Eliminar/Ingreso borrador, Revertir/Ingreso y Confirmar/Salida. No se genera un evento desde el móvil ni se promete registrar todos los CRUD. Backend ya aplica migración 15 en la base local según comprobación previa, pero otras bases deben verificarse por separado.

Logger de frontend registra código/operación/correlación/UTC, no token, contraseña, URL, request body ni excepción. Frontera Razor usa LoggedErrorBoundary y mensajes recuperables; confirmar la recuperación por cambio de ruta en QA. Logger Serilog del backend es independiente. Existe una prueba backend en ActualizacionUsuariosTests que fuerza error controlado en una base Testcontainers descartable y verifica archivo de log; no debe ejecutarse contra la base personal ni introducir errores de SQL en ella para una demo.

## Verificaciones y pendientes

Ver VALIDACION.md para resultados ejecutados, comandos y límites. Las pruebas del frontend compilan fuentes portables reales sin exigir dispositivo, y no sustituyen SecureStorage, diálogos, selector, visor PDF ni el ciclo de vida Android. No afirmar recepción por correo, prueba visual Android o integración total de Comercio antes de realizarlas.

## Funciones deliberadamente fuera de alcance

Sin registro público, recuperación de clave, refresh token, cambio de rol por PUT, restauración de bajas, endpoint de logs, cobro real ni ranking de ventas. Altas alternativas /usuarios, /empleados y /clientes existen, pero la UI utiliza el registro unificado. No se duplicaron pantallas para cubrir endpoints redundantes.
