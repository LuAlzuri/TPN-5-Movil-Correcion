# CUIT y CUIL únicos en clientes y proveedores

Cada CUIT/CUIL puede pertenecer a un solo Cliente y a un solo Proveedor.
La unicidad es independiente entre ambos grupos: se permite un Cliente y un
Proveedor con el mismo identificador. La regla incluye los registros eliminados
lógicamente. No establece unicidad para Empleados ni Administradores.

Se conserva el nombre CUIT de las entidades y de los contratos JSON. Para
comparar se ignoran guiones, espacios ASCII, tabulaciones y saltos de línea
(CR y LF). Por ejemplo, `20-12345678-9`, `20123456789` y `20 12345678 9`
representan el mismo identificador. No se incorpora validación de dígito
verificador ni se impone una longitud exacta de 11 dígitos en esta entrega.

## Protección en las rutas y en la base

Las validaciones se reutilizan desde CuitUnico. Rechazan con 400 valores vacíos,
sin identificador después de quitar separadores, o de más de 20 caracteres.
Los duplicados detectados devuelven 409.

Las rutas cubiertas son el registro de Cliente mediante `/api/auth/register`,
la edición mediante `/api/clientes/{id}` y `/api/usuarios/{id}`, y la creación
y edición mediante `/api/proveedores`. Al editar se excluye el propio registro.
Se comprueban también las cuentas y proveedores eliminados. Los conflictos de
índices únicos SQL Server (2601/2627) se traducen a 409 si una operación concurrente
supera la consulta previa.

Se agrega una columna calculada persistida CUITNormalizado en Usuarios y otra en
Proveedores. No se reescribe el CUIT original para calcular estas claves, ni se
exponen las columnas nuevas en las respuestas de la API. Las escrituras existentes
conservan su formato y eliminan espacios exteriores.

Usuarios tiene un índice único sobre CUITNormalizado filtrado por Rol = Cliente,
sin excluir Eliminado. Proveedores tiene un índice único sin filtro sobre su
CUITNormalizado, además del índice original sobre CUIT. Estas restricciones
impiden duplicados también si se escriben datos sin usar los controladores.

## Migraciones y datos existentes

Se generaron dos migraciones reales, una por entidad modificada:

- `20261026210457_CuilUnicoClientes`: columna calculada e índice de Usuarios.
- `20261026210619_CuilUnicoProveedores`: columna calculada e índice de Proveedores.

Ambas conservan los registros y sus relaciones. El snapshot quedó actualizado.
No se aplicaron a una base del usuario. Las consultas de la nueva versión
requieren las columnas nuevas, por lo que debe coordinarse su aplicación antes
de utilizar esa versión contra una base existente.

Si ya hay identificadores equivalentes repetidos, la creación del índice único
fallará. No hay borrado, combinación ni corrección automática de duplicados.
Primero debe verificarse el destino y resolver los casos con una decisión explícita.
La siguiente consulta de solo lectura permite identificarlos antes de aplicar:

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

## Verificación y alcance

Resultado local: 72 pruebas aprobadas, 0 fallidas y 0 omitidas. Los casos nuevos
cubren registro, rutas alternativas de edición, proveedores, formatos equivalentes,
eliminados, exclusión del propio ID, independencia de grupos, otros roles,
entradas inválidas y rechazo directo por los índices sin validación de aplicación.
Las pruebas funcionales usaron SQLite aislada.

El proyecto tools/Schema verificó las 13 migraciones, los índices y las relaciones
sin conectarse a una base. La suite confirmó que el modelo SQL Server coincide con
el snapshot y se generó y revisó el SQL de las nuevas migraciones. No se ejecutó
ese SQL ni se probaron estas migraciones contra un servidor SQL Server real.

Este ajuste no completa el paso 9.3 de CRUD de clientes. Su creación en
`POST /api/clientes` continúa temporalmente en 501; la creación funcional de
cuentas Cliente sigue disponible mediante el registro autenticado existente.
