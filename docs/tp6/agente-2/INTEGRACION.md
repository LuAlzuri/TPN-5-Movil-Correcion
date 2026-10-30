# Integración del Plan 2

Implementación de catálogo, compras e inventario dentro de la misma app MAUI Blazor Hybrid del Plan 1. No se agregaron endpoints, tablas, migraciones ni seeds al backend. El servidor sigue siendo la autoridad de roles, stock, precios, borradores, confirmaciones y auditoría.

## Pantallas y contratos

| Pantalla | Solicitud | Comportamiento |
|---|---|---|
| /tienda | GET /productos, GET /categorias/catalogo | Búsqueda, filtro por categoría y paginación de 12. Conserva filtros al abrir un detalle. |
| /tienda/productos/{id} | GET /productos/{id} | Imagen con respaldo, precio, stock, cantidad y alta en el borrador. |
| /compras/carrito | GET/POST/PUT/DELETE /salidas | Recupera o crea el borrador propio, ajusta cantidades, quita líneas y calcula el total. Editar a 0 usa 204. |
| /compras/pago/{salidaId} | GET /salidas/{id}, POST /salidas/{id}/confirmar-cliente | Pago simulado con Tarjeta, Transferencia o Efectivo; envía el pedido real y limpia el borrador local. |
| /compras/pedidos | GET /salidas | Pedidos propios con estado, paginación y acceso a borradores. |
| /compras/pedidos/{id} | GET /salidas/{id}, DELETE /salidas/{id} | Detalle; cancelar/descartar solo cuando la regla del servidor lo permite. |
| /inventario/productos | GET /productos, DELETE /productos/{id} | Listado, búsqueda, filtro de categoría y baja lógica confirmada. |
| /inventario/productos/nuevo | POST /productos | Alta sin stock ni imagen; navega al detalle creado. |
| /inventario/productos/{id} | GET/PUT /productos/{id}, POST /productos/{id}/imagen | Edición y carga opcional de imagen después de crear; máximo 5 MiB. |
| /inventario/proveedores | GET /proveedores, DELETE /proveedores/{id} | Listado, búsqueda y baja confirmada. |
| /inventario/proveedores/nuevo, /{id} | POST /proveedores, GET/PUT /proveedores/{id} | Alta y edición de nombre, CUIT, email, teléfono y dirección. |
| /inventario/ingresos | GET /ingresos, GET /proveedores/catalogo | Filtros por proveedor y fechas; muestra borrador/confirmado y total. |
| /inventario/ingresos/nuevo | POST /ingresos | Crea la cabecera y navega a la edición de líneas. |
| /inventario/ingresos/{id} | GET/POST/PUT/DELETE /ingresos/{id}, /detalles, /confirmar | Agrega, edita o quita líneas; confirma o revierte con confirmación explícita. |
| /inventario/solicitudes | GET /salidas | Pedidos de clientes, incluyendo borradores, con estado y total. |
| /inventario/solicitudes/{id} | GET /salidas/{id}, POST /salidas/{id}/confirmar | Detalle y aceptación por Empleado; el stock se descuenta en el servidor. |

## Roles

- Cliente: tienda, carrito, pago y pedidos propios.
- Empleado: consulta y opera productos, proveedores, ingresos y solicitudes. Puede crear/editar/confirmar ingresos y aceptar pedidos.
- Administrador: consulta las pantallas de inventario y solicitudes; no crea ni modifica ingresos desde el móvil.

Las páginas usan `[Authorize(Roles = ...)]`. El menú muestra Comercio al Cliente y las secciones de inventario al Personal. Las acciones de escritura se ocultan o deshabilitan fuera del rol permitido, pero el backend vuelve a validar.

## Límites del contrato

- `ProductoResponse` no incluye proveedor. Ingresos y salidas no incluyen nombres de producto, proveedor, empleado o cliente. La UI resuelve nombres con catálogos y usa `Producto #ID`, `Proveedor #ID`, `Empleado #ID` o `Cliente #ID` cuando no corresponde consultarlos.
- `GET /clientes/catalogo` es solo Administrador. Un Empleado ve el identificador del cliente en solicitudes.
- El listado de salidas del Personal devuelve también borradores de clientes; se muestran con su estado para no ocultar información que el servidor entrega.
- Agregar un producto repetido a una salida reemplaza la cantidad; en ingresos se permiten líneas repetidas. La UI no fuerza ninguna de esas reglas.
- No hay endpoint para borrar la imagen de un producto. Se puede reemplazar subiendo otra.
- La auditoría comercial se genera solo en el backend. El móvil no envía eventos.
- Los filtros de fecha se envían como límites UTC−03 inclusivos mediante `ComercioDates`, igual que auditoría.

## Errores y estado

`ApiFailure` conserva el mensaje y el código HTTP. Los formularios bloquean doble envío mientras hay una operación en curso, muestran confirmaciones para bajas/confirmaciones y ofrecen reintentar cuando la consulta falla. Un 409 de stock se muestra sin reintentar la escritura automáticamente. Las escrituras no se repiten ante resultados inciertos.

## Fecha de registro

Implementación registrada el 30 de octubre de 2026 sobre la rama `auditoria`, base `567baca`.
