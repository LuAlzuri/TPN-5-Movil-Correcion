# Validación del Plan 1

Registro del 29 de octubre de 2026. Rama de trabajo `codex/plan1-base-administracion`, basada en main f65e069. Este registro describe las verificaciones técnicas de la implementación; su publicación no implica aprobar los pendientes. El PR 29 anterior solo correspondía a carpetas y ya fue incorporado por el propietario.

## Resultados ejecutados

| Verificación | Resultado |
|---|---|
| Restauración de dependencias MAUI y Bootstrap oficial local | Correcta |
| Compilación Android con SDK instalado, última versión del código | Correcta, 0 errores y 0 advertencias |
| Pruebas unitarias de fuentes reales portables | 21 aprobadas, 0 fallidas, 0 omitidas |
| Solución agrupadora | API, app MAUI y tests incluidos en Apinventario.slnx |
| Preparación de vista web externa | Host Blazor disponible en localhost:5090; componentes copiados del proyecto para QA, no otro entregable |
| API local y recurso interactivo Blazor | HTTP 200 verificados desde entorno con acceso a localhost |
| Pantalla login e inicio administrativo en vista web | Renderizados y un acceso real a inicio Personal observado; revisión completa del resto de pantallas pendiente |
| Bases, migraciones y backend en esta implementación | Sin cambios |

La primera compilación señaló que el entorno buscaba Android SDK en C:\Android-SDK; el SDK real está en Program Files (x86). Se corrigió el comando con AndroidSdkDirectory, sin cambiar el entorno global. Los marcadores .gitkeep se excluyeron de imágenes/fuentes para que no se procesaran como recursos Android.

Una ejecución de tests dentro del sandbox no pudo conectar vstest con testhost; se repitió fuera de esa restricción y los 21 casos pasaron. No confundir esa interrupción de infraestructura con una aserción fallida.

## Qué cubren las 21 pruebas

- Bearer correcto y respuesta 204 sin deserialización JSON.
- 401 elimina sesión; 403 conserva token/estado válido.
- Token vencido impide llamar al servidor.
- ValidationProblemDetails preserva errores de campos.
- Logs omiten token, contraseña y detalles sensibles de respuesta.
- Retry-After se conserva para 429.
- Auditoría utiliza total, soporta id long y calcula paginación.
- Actualizar cuenta sin password no envía ese campo ni rol.
- Alta de empleado no envía datos exclusivos del perfil Cliente.
- Rango inválido de reportes no produce HTTP.
- Descarga exige cabecera PDF real, no solo MIME.
- Rutas absolutas, de otro origen o con recorrido de directorio se rechazan antes de adjuntar token.
- Origen API rechaza credenciales, queries y HTTP público.
- Límites de día de auditoría son explícitos UTC−03.
- Restauración usa el perfil de servidor; logout borra la sesión.
- Login no envía token anterior y guarda la nueva sesión.
- Errores 5xx no revelan detalles internos.

Los Theory con varios casos cuentan como pruebas separadas. Se compilan las fuentes portables reales con un HttpMessageHandler de ensayo; no se mockea el comportamiento que se está afirmando verificar. Los adaptadores nativos SecureStorage, diálogos y archivos requieren prueba en dispositivo.

## Android

Se compiló y se instaló una primera versión Debug en un emulador configurado. El arranque detectó falta de ensamblados debido al despliegue rápido por defecto de la plantilla. Se añadió EmbedAssembliesIntoApk=true y la versión final compiló correctamente. El propietario rechazó la reinstalación y pidió probar las pantallas con Blazor; no se repitió la instalación ni se declara que el APK final haya sido probado visualmente en Android.

La herramienta de captura de la ventana del emulador también informó que el recorte quedaba fuera del monitor. No hay evidencia visual Android válida. Pendiente, con autorización del propietario: instalar el APK final, validar arranque, login/roles, teclado, Atrás, safe areas, SecureStorage y abrir/compartir PDF. El recorrido de Comercio requiere además el módulo del Plan 2.

## Vista web de desarrollo

Se preparó un host temporal fuera del repositorio para reutilizar los componentes Razor y clientes de Plan 1. Usa sesión en memoria, confirmaciones de navegador y descarga de PDF; no representa el comportamiento nativo de MAUI. No se añadió a Apinventario.slnx.

El login llegó al inicio Personal con la API real. La revisión posterior no pudo completarse por problemas de recuperación/control de las pestañas de prueba y navegación a un módulo de Comercio aún inexistente. Por tanto no se afirma un recorrido visual completo de usuarios, categorías, auditoría o reportes. Los enlaces de módulos pendientes se deben verificar también dentro del host MAUI; una ruta desconocida en el host temporal puede producir un 404 de servidor, distinto del NotFound del Router Hybrid.

## SMTP

Configuración local revisada: SMTP deshabilitado y host sin configurar. El destinatario personal proporcionado por el propietario está autorizado solo como destino de ensayo y no se incorporó a los archivos de código/Git. No se envió correo. Falta configurar remitente SMTP seguro en servidor y realizar una prueba real de recepción de adjunto; un test de servicio o descarga PDF no sustituye esa prueba.

## Logger de backend

Existe la prueba `ActualizacionUsuariosTests.Error_inesperado_se_registra_sin_exponer_credenciales_ni_archivos`, que utiliza SQL Server Testcontainers descartable y verifica el log Serilog. Docker está instalado, pero `docker info` informó que no existe la conexión al motor DockerDesktopLinuxEngine: no se ejecutó esa prueba. No se provocaron fallos ni se agregaron restricciones en Apinventario_Local. La comprobación previa de los eventos de auditoría comercial es independiente de este log técnico.

## Pendientes antes de aprobar la entrega completa

1. Revisión visual integral del Plan 1 y pruebas de formularios con errores/confirmaciones en el host elegido.
2. Prueba final Android del APK actual; navegador no acredita el requisito docente.
3. Configurar SMTP y comprobar recepción del PDF autorizado.
4. Iniciar Docker y ejecutar la prueba existente de logs del backend en entorno descartable.
5. Integrar Plan 2 y repetir compra/entrada/stock/auditoría/reportes como una sola aplicación.
6. Revisión conjunta del análisis de normalización, especialmente columnas calculadas y snapshots.

La implementación del Plan 1 y sus contratos están disponibles para continuar; estos pendientes se mantienen explícitos y no se marcan como completados.
