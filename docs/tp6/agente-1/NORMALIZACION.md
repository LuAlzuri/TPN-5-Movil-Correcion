# Revisión de normalización del modelo del TP5 para TP6

Revisión estática de Entities, AppDbContext y migraciones hasta AuditoriaAcciones. No se modificó el esquema. Este documento justifica el modelo y explicita límites; no reemplaza un análisis docente de reglas de negocio ni certifica automáticamente 3FN por tener identificadores artificiales.

## Relaciones y claves

| Relación | Claves y restricciones | Dependencias funcionales relevantes |
|---|---|---|
| Usuarios | PK Id; Email UNIQUE; CUITNormalizado UNIQUE solo en filas Cliente | Id → nombre, apellido, CUIT, email, hash, rol, eliminado. Email también identifica una cuenta. CUIT no es clave candidata global de Usuarios. |
| Empleados | PK Id compartida y FK Usuarios.Id | Id → FechaIngreso. Una cuenta tiene a lo sumo un perfil Empleado. |
| Clientes | PK Id compartida y FK Usuarios.Id | Id → Telefono, Direccion. Una cuenta tiene a lo sumo un perfil Cliente. |
| Categorias | PK Id; Nombre UNIQUE | Id → Nombre, Descripcion, FechaCreacion, Eliminado. Nombre es clave alternativa según restricción vigente. |
| Proveedores | PK Id; CUIT y CUITNormalizado únicos | Id → datos de proveedor. CUIT identifica proveedor; el valor normalizado evita variantes equivalentes. |
| Productos | PK Id; FK CategoriaId | Id → nombre, descripción, precio actual, stock, categoría y estado. CategoriaId → datos propios de categoría, almacenados fuera de Productos. Nombre no es único. |
| Imagenes | PK Id; ProductoId UNIQUE y FK | Id y ProductoId son claves alternativas. Cada producto tiene cero o una imagen con ruta, MIME, tamaño y nombre. |
| Ingresos | PK Id; FK ProveedorId, EmpleadoId y EmpleadoConfirmacionId opcional | Id → datos de cabecera, creador, confirmador y estado. Un proveedor/empleado puede aparecer en muchos ingresos. |
| DetallesIngreso | PK Id; FK IngresoId y ProductoId | Id → ingreso, producto, cantidad, precio histórico y descuento. No hay UNIQUE(IngresoId,ProductoId): no asumir esa pareja como clave candidata; puede haber varias líneas. |
| Salidas | PK Id; FK ClienteId y UsuarioConfirmacionId opcional | Id → cabecera, propietario, etapas y confirmador. Un cliente tiene muchas salidas. |
| DetallesSalida | PK Id; UNIQUE(SalidaId,ProductoId); dos FK | Id → datos de línea; (SalidaId,ProductoId) es clave alternativa y determina cantidad/precio/descuento de esa línea. |
| AuditoriaAcciones | PK Id long; índices no únicos FechaUtc, (Entidad,EntidadId), (UsuarioId,FechaUtc) | Id → actor referenciado, email histórico, evento, entidad/id de origen, fecha, IP y observación. Los índices de búsqueda no son claves candidatas. |

Todas las FK configuradas usan Restrict; las bajas de negocio son lógicas. La relación de perfiles usa PK compartida. La consistencia rol/perfil también depende de validación de servicios y no solo de la estructura de tablas.

## Primera forma normal

Los encabezados y líneas están separados. No se almacena un conjunto de productos en una columna del ingreso/salida ni una lista de categorías en Productos. Teléfono/dirección se consideran valores simples conforme al alcance de esta aplicación; si se requiriesen varios teléfonos con propiedades independientes, sería otro modelo. La ruta de imagen y Observacion de auditoría no son colecciones relacionales escondidas.

## Segunda forma normal

No basta con observar PK artificial. Revisando las alternativas declaradas: Email determina una cuenta; Nombre determina Categoría; CUIT identifica Proveedor; ProductoId identifica una imagen. En DetallesSalida la cantidad y los importes corresponden a la pareja (SalidaId,ProductoId), no a una salida sola ni al producto aislado. Datos de cliente, proveedor y producto no se repiten como atributos de sus detalles. En DetallesIngreso solo se garantiza unicidad de Id; el modelo no autoriza deducir una clave compuesta de dos columnas sin regla adicional.

Los precios de línea no dependen del producto actual solamente: son valores capturados para ese movimiento en su momento. Cambiar el precio vigente no debe reescribirlos. Interpretarlos como precio actual llevaría a una dependencia errónea y destruiría el historial comercial.

## Tercera forma normal

Productos no repite el nombre/descripción de categoría: los obtiene por FK. Ingresos no repite dirección/email de proveedor ni los datos del empleado. Salidas no repite el perfil completo de cliente o confirmador. Los detalles no guardan nombres actuales de productos ni totales de encabezado. La API calcula subtotales/totales para respuesta y PDF, en lugar de añadir a cada línea los atributos de otras entidades.

### Redundancias y decisiones que deben justificarse

- **Stock en Productos:** es saldo materializado para validar disponibilidad de forma transaccional. IdProducto determina el saldo; no se deduce una violación formal de 3FN solo por poder reconstruirlo desde otras relaciones. Sí existe un riesgo de consistencia de saldo: únicamente ingresos/ventas/reversiones autorizados deben modificarlo, con transacciones y comprobación de no negatividad. No editarlo desde formularios de producto.
- **CUITNormalizado:** es una columna computada persistida a partir de CUIT. Si se trata como atributo relacional independiente, existe una dependencia CUIT → CUITNormalizado que debe discutirse. Técnicamente es una proyección determinista gestionada por SQL para índice único, no una segunda entrada editable. Presentarla como excepción física deliberada; no afirmar que una relación con ambos valores independientes está estrictamente normalizada sin esta salvedad.
- **Precio y descuento históricos:** son hechos del movimiento, no copias del precio actual. El requisito de preservar valores comerciales justifica su ubicación en la línea. Subtotal y total se calculan; no hay dependencia transitiva almacenada con ellos.
- **UsuarioEmail de auditoría:** debe interpretarse como email del actor en el momento de ese evento, no email actual dependiente siempre de UsuarioId. Es un snapshot. La entidad guarda UsuarioId y una referencia genérica Entidad/EntidadId sin FK real a cada tabla: eso limita integridad referencial, pero no prueba por sí mismo una violación de 3FN. No inferir UsuarioId → email histórico entre todos los eventos si el email puede cambiar. Si la intención fuera conservar siempre el email actual, sí habría redundancia evitable.
- **Creador y confirmador:** son papeles distintos aunque a veces coincidan, con fechas y FK diferentes; no deben colapsarse por coincidencias observadas en datos demo.

## Conclusión y evidencias requeridas

El núcleo separa cuentas/perfiles, maestros, encabezados y detalles y permite justificar las formas normales con las dependencias anteriores. Para una defensa académica rigurosa deben presentarse también las decisiones físicas de stock, CUIT computado y snapshots, sus restricciones y anomalías potenciales. No asegurar «3FN estricta sin excepciones» sin acordar cómo se modelan los atributos calculados e históricos.

La evidencia de saldo debe cubrir: confirmar ingreso, enviar carrito sin descontar, aceptar salida, impedir stock negativo, reversión permitida y rollback por stock insuficiente. Las comprobaciones previas de auditoría verificaron estos tipos de comportamiento en datos locales de ensayo; la integración móvil de Comercio debe repetir su recorrido con el Plan 2. El responsable de Plan 2 debe revisar las dependencias de detalles según las reglas reales y registrar observaciones en su propia carpeta; el Plan 1 consolida este documento.

Si el docente exige eliminar una redundancia física concreta o agregar integridad de referencias de auditoría, preparar una propuesta de backend y migración separada para aprobación. El frontend no corrige el modelo silenciosamente.
