# Apinventario App

Estructura de carpetas para el frontend del TP6, inspirada en InventarioApp del repositorio https://github.com/AgusBuss/tienda-de-negocios- y adaptada a los dos planes de trabajo.

Esta carpeta contiene el proyecto MAUI Blazor Hybrid Android del Plan 1, con acceso, roles, administración, auditoría y reportes. La solución raíz `Apinventario.slnx` agrupa esta app y la API. Los tres roles comparten la misma app. Las pantallas de comercio corresponden al Plan 2 y se integran en las carpetas asignadas.

Los contratos compartidos, instrucciones de ejecución, mapa HTTP y análisis de normalización están en `docs/tp6/agente-1/`, desde la raíz. Los `.gitkeep` conservan carpetas todavía vacías. No generar otra plantilla sobre este proyecto.

## Reparto de trabajo

| Ubicación | Responsable |
|---|---|
| Components/Layout y Components/Shared | Agente 1 |
| Components/Pages/Acceso, Administracion, Auditoria y Reportes | Agente 1 |
| Services/Infrastructure y Services/Administracion | Agente 1 |
| Models/Common y Models/Administracion | Agente 1 |
| Platforms, Resources, Properties y wwwroot | Agente 1 |
| Components/Pages/Tienda, Compras e Inventario | Agente 2 |
| Services/Comercio y Models/Comercio | Agente 2 |

El agente 1 será dueño del csproj, MauiProgram, App/MainPage, Router, imports globales, configuración y solución. El agente 2 utilizará estilos `.razor.css` e imports locales dentro de sus carpetas. Los contratos comunes se acuerdan antes de conectar el módulo de Comercio; no duplicar sesión ni cliente HTTP.

Las carpetas de apoyo se encuentran en `tests/Apinventario.App.Unit/Agente1`, `tests/Apinventario.App.Unit/Agente2`, `docs/tp6/agente-1` y `docs/tp6/agente-2`, relativas a la raíz del repositorio. El proyecto de tests ejecuta las fuentes portables reales de infraestructura y administración sin depender de Android.

## Criterios de implementación

- MAUI Blazor Hybrid para Android, integrado mediante HTTP con la API existente en la carpeta hermana `TPN°5-Movil`.
- Models contiene contratos HTTP; no entidades de Entity Framework ni conexiones SQL.
- No crear otro repositorio Git dentro de esta carpeta.
- No modificar backend, migraciones o permisos para resolver diferencias de contratos sin autorización.
- La consigna TP6 y los planes actualizados definen el alcance. Las limitaciones de prueba y despliegue se registran en la documentación del Plan 1; no se afirma funcionalidad del Plan 2 antes de su integración.
