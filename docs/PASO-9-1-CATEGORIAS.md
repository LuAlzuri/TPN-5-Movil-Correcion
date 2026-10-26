# Paso 9.1 Categorías

El CRUD de `/api/categorias`, incluyendo listado paginado y consulta por ID,
requiere Administrador. `/api/categorias/catalogo` requiere autenticación y admite
Administrador, Empleado y Cliente: devuelve únicamente `id` y `nombre` de las
categorías activas, ordenadas por nombre e ID. Sirve para seleccionar la categoría
al cargar productos y para los futuros filtros del catálogo de productos.

## Validación y consultas

El nombre es obligatorio, no admite solo espacios y acepta hasta 100 caracteres.
La descripción es opcional y acepta hasta 1000 caracteres. Ambos valores se guardan
sin espacios exteriores; una descripción vacía se guarda como null.

Los nombres duplicados devuelven 409, incluso si la categoría anterior fue eliminada.
La comparación usa la misma columna y collation que el índice único existente:
no se impone una regla distinta de mayúsculas o acentos en memoria. Las colisiones
del índice único en SQL Server también se traducen a 409 al guardar.

El listado administrativo acepta `page` positivo y `pageSize` entre 1 y 100
(predeterminados 1 y 10). Los parámetros inválidos y los desplazamientos que
exceden Int32 devuelven 400. El orden es nombre e ID; `busqueda` elimina espacios
exteriores antes de buscar por nombre. Las páginas fuera del resultado devuelven
una lista vacía.

## Borrado lógico y relaciones

Eliminar una categoría marca `Eliminado`; conserva productos y claves foráneas.
La categoría desaparece de las consultas operativas y no puede editarse ni
eliminarse nuevamente (404). La creación y edición de productos ya comprueba
la existencia de la categoría mediante el filtro global: una categoría eliminada
no puede seleccionarse en estas operaciones. Las pruebas verifican este comportamiento.

No se cambió el modelo ni el esquema y no se genera migración. No se incorporan
nuevos endpoints de auditoría ni se modifican las consultas históricas de movimientos.

## Verificación

`dotnet test tests/Auth.Integration/Auth.Integration.csproj` ejecuta las pruebas
de autenticación y categorías. Usar la configuración predeterminada SQLite aislada;
no configurar `AUTH_TEST_SQLSERVER_CONNECTION` para una base personal.

Las pruebas de categorías cubren permisos de los tres roles y anónimos, catálogo
activo y mínimo, entradas inválidas, paginado, búsqueda, creación, edición,
duplicados activos y eliminados, borrado lógico, conservación de productos,
rechazo de asociaciones a categorías eliminadas y ausencia de cambios de modelo.

Resultado local: 34 pruebas aprobadas, 0 fallidas y 0 omitidas (incluye las 25
de autenticación). Los casos funcionales usaron SQLite aislada. La comparación
del modelo usó el proveedor SQL Server sin abrir conexión; no se ejecutaron
pruebas funcionales contra SQL Server en esta entrega.
