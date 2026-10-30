param(
    [string]$BaseUrl = 'http://127.0.0.1:5087',
    [Parameter(Mandatory = $true)][string]$Password
)
$ErrorActionPreference = 'Stop'
# Ejecutar una sola vez sobre la base local vacia, luego del bootstrap del administrador.
function Api($method, $path, $body, $token) {
    $args = @{ Method=$method; Uri="$BaseUrl$path"; UseBasicParsing=$true }
    if ($token) { $args.Headers = @{ Authorization="Bearer $token" } }
    if ($null -ne $body) { $args.Body = $body | ConvertTo-Json -Depth 8; $args.ContentType='application/json' }
    $response = Invoke-WebRequest @args
    Write-Host "$($response.StatusCode) $method $path"
    if ($response.Content) { return $response.Content | ConvertFrom-Json }
}
function Login($email) { (Api POST '/api/auth/login' @{email=$email; password=$Password} $null).token }
function Check($condition, $message) { if (!$condition) { throw $message }; Write-Host "PASS: $message" }
$admin = Login 'admin@demo.test'
$null = Api GET '/api/auth/me' $null $admin
$null = Api POST '/api/auth/register' @{nombre='Empleado'; apellido='Demo'; cuit='2'; email='empleado@demo.test'; password=$Password; rol='Empleado'; fechaIngreso='2026-10-27'} $admin
$null = Api POST '/api/auth/register' @{nombre='Cliente'; apellido='Demo'; cuit='3'; email='cliente@demo.test'; password=$Password; rol='Cliente'; telefono='1111111111'; direccion='Direccion de prueba 123'} $admin
$category = Api POST '/api/categorias' @{nombre='Libreria Demo'; descripcion='Semilla local de prueba'} $admin
$supplier = Api POST '/api/proveedores' @{nombre='Proveedor Demo'; cuit='30-00000001-0'; email='proveedor@demo.test'} $admin
$product = Api POST '/api/productos' @{nombre='Cuaderno Demo'; descripcion='Producto para pruebas locales'; precio=1500; categoriaId=$category.id} $admin
$other = Api POST '/api/productos' @{nombre='Lapicera Demo'; precio=500; categoriaId=$category.id} $admin
Check ($product.stock -eq 0) 'Stock inicial cero'
$employee = Login 'empleado@demo.test'
$income = Api POST '/api/ingresos' @{proveedorId=$supplier.id; observaciones='Ingreso de prueba: 10 cuadernos'} $employee
$null = Api POST "/api/ingresos/$($income.id)/detalles" @{productoId=$product.id; cantidad=10; descAplicado=0} $employee
$null = Api POST "/api/ingresos/$($income.id)/confirmar" $null $employee
$stock = Api GET "/api/productos/$($product.id)" $null $admin
Check ($stock.stock -eq 10) 'Ingreso confirmado: stock 10'
$client = Login 'cliente@demo.test'
$sale = Api POST '/api/salidas' @{observaciones='Venta de prueba: 2 cuadernos'} $client
$null = Api POST "/api/salidas/$($sale.id)/detalles" @{productoId=$product.id; cantidad=2} $client
$null = Api POST "/api/salidas/$($sale.id)/confirmar-cliente" $null $client
$stock = Api GET "/api/productos/$($product.id)" $null $admin
Check ($stock.stock -eq 10) 'Pedido del cliente no descuenta stock antes de aceptarlo'
$null = Api POST "/api/salidas/$($sale.id)/confirmar" $null $employee
$stock = Api GET "/api/productos/$($product.id)" $null $admin
Check ($stock.stock -eq 8) 'Venta confirmada: stock final 8'
foreach ($case in @(@{path='/api/productos'; token=$null; expected=401}, @{path='/api/usuarios'; token=$client; expected=403})) {
    try { $null = Api GET $case.path $null $case.token; throw 'Se esperaba rechazo HTTP' }
    catch { Check ([int]$_.Exception.Response.StatusCode -eq $case.expected) "Control de acceso HTTP $($case.expected)" }
}
foreach ($report in @('stock','ingresos','salidas')) {
    $response = Invoke-WebRequest -UseBasicParsing -Uri "$BaseUrl/api/reportes/$report" -Headers @{Authorization="Bearer $admin"}
    Check ($response.StatusCode -eq 200 -and $response.Headers['Content-Type'] -like 'application/pdf*') "Reporte PDF $report"
}
Write-Host "Semilla completa. Producto $($product.id), ingreso $($income.id), salida $($sale.id)."
