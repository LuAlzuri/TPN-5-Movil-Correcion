# Validación del Plan 2

Registro del 30 de octubre de 2026. Rama `auditoria`, base `567baca`. Se validó compilación y pruebas automatizadas; no se declara una prueba visual Android completa.

## Resultados ejecutados

| Verificación | Resultado |
|---|---|
| Compilación Android de `Apinventario.App` para `net10.0-android` | Correcta, 0 errores y 0 advertencias |
| Pruebas unitarias de fuentes portables reales | 32 aprobadas, 0 fallidas, 0 omitidas |
| Pruebas nuevas del Plan 2 | 11 aprobadas |
| Backend, migraciones y seeds | Sin cambios |
| Registro de servicios de Comercio en `MauiProgram` | Correcto |
| Rutas de Comercio y enlaces del menú | Presentes para los roles correspondientes |

Comandos usados desde la raíz:

```powershell
dotnet build Apinventario.App/Apinventario.App.csproj -f net10.0-android
dotnet test tests/Apinventario.App.Unit/Apinventario.App.Unit.csproj
```

## Cobertura de las 11 pruebas del Plan 2

- Paginación, búsqueda y filtro de categoría del catálogo.
- Alta de producto sin stock ni imagen.
- Carga multipart con campo `File` y tipo de contenido declarado.
- Editar una cantidad de salida a 0 sin intentar leer JSON de un 204.
- Reutilización de un borrador preferido sin listar.
- Recuperación del borrador más nuevo cuando el preferido ya no existe.
- Exclusión de ventas confirmadas o eliminadas al buscar borradores.
- Creación de ingreso con proveedor y observación recortada.
- Catálogo y detalle de clientes en las rutas esperadas.
- Actualización de línea de ingreso sin enviar `productoId` (el servidor no lo acepta en ese PUT).
- Límites de día comercial UTC−03 inclusivos.

Las pruebas compilan las fuentes portables reales con un `HttpMessageHandler` de ensayo. No cubren Razor, diálogos, selector de archivos, SecureStorage ni ciclo de vida Android.

## Límites de esta validación

- No se instaló ni recorrió el APK en emulador o dispositivo en esta etapa. La compilación no sustituye la evidencia visual Android.
- No se ejecutó una compra completa contra la API real desde Android.
- No se verificó la recepción de correo ni se modificó SMTP; eso pertenece al Plan 1 y sigue pendiente.
- Los avisos de stock insuficiente, las confirmaciones y el comportamiento del teclado/Atrás requieren prueba manual.

## Pendientes antes de cerrar la entrega

1. Instalar el APK actual en un emulador o dispositivo autorizado y recorrer los tres roles.
2. Probar alta de producto con imagen, ingreso con varias líneas, confirmación, reversión y conflicto de stock.
3. Probar compra completa: catálogo, carrito, pago simulado, pedido enviado y aceptación por Empleado.
4. Revisar solicitudes con nombre de cliente como Administrador y con `Cliente #ID` como Empleado.
5. Repetir auditoría y reportes después de las operaciones del Plan 2.
6. Confirmar que la app no expone errores internos y que las confirmaciones funcionan en Android.
