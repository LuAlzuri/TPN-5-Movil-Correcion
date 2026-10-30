# Ejecución del Plan 2

La app sigue siendo `Apinventario.App`, dentro de `Apinventario.slnx`. No hay una segunda app por rol ni un host alternativo en el repositorio.

## Compilar y probar

Desde la raíz:

```powershell
dotnet build Apinventario.App/Apinventario.App.csproj -f net10.0-android
dotnet test tests/Apinventario.App.Unit/Apinventario.App.Unit.csproj
```

Si el SDK Android no está en la ruta detectada, usar el parámetro local documentado en `docs/tp6/agente-1/EJECUCION.md`; no cambiar ajustes globales.

## Iniciar la API

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ASPNETCORE_URLS='http://127.0.0.1:5087'
dotnet run --project 'TPN°5-Movil/TPN°5-Movil.csproj' --no-launch-profile
```

En emulador Android el origen Debug es `http://10.0.2.2:5087`. Para un teléfono físico se necesita un origen HTTPS accesible o una IP LAN autorizada; no se modifica la configuración de red por este Plan 2.

## Recorrido sugerido

1. Ingresar como Cliente: `/tienda`, agregar productos y pasar por `/compras/carrito`, `/compras/pago/{id}` y `/compras/pedidos`.
2. Ingresar como Empleado: `/inventario/ingresos`, crear un ingreso, agregar líneas y confirmarlo; luego revisar `/inventario/solicitudes` y aceptar el pedido del Cliente.
3. Ingresar como Administrador: consultar productos, proveedores, ingresos y solicitudes; verificar que no ofrece crear ni modificar ingresos.
4. Revisar `/inventario/productos` después de las operaciones para comprobar el stock y la baja lógica.
5. Revisar auditoría y reportes del Plan 1 para confirmar el impacto de las operaciones comerciales.

## Rutas del Plan 2

| Ruta | Roles |
|---|---|
| `/tienda`, `/tienda/productos/{id}` | Cliente |
| `/compras/carrito`, `/compras/pago/{salidaId}`, `/compras/pedidos`, `/compras/pedidos/{id}` | Cliente |
| `/inventario/productos`, `/inventario/productos/nuevo`, `/inventario/productos/{id}` | Administrador, Empleado |
| `/inventario/proveedores`, `/inventario/proveedores/nuevo`, `/inventario/proveedores/{id}` | Administrador, Empleado |
| `/inventario/ingresos`, `/inventario/ingresos/nuevo`, `/inventario/ingresos/{id}` | Administrador, Empleado |
| `/inventario/solicitudes`, `/inventario/solicitudes/{id}` | Administrador, Empleado |

No se requieren migraciones ni cuentas nuevas para este plan. Usar las cuentas de desarrollo existentes y no incorporar contraseñas, tokens o destinatarios personales al repositorio.
