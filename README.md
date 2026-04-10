# FactuSync 🚀

FactuSync es una solución de integración moderna entre **Factusol** (Desktop) y una interfaz móvil/web basada en **Blazor WebAssembly** y **ASP.NET Core**. Permite la gestión remota de clientes, artículos y la creación de pedidos en tiempo real directamente sobre la base de datos de Factusol.

## ✨ Características Principales

- **Sincronización en Tiempo Real**: Conexión directa con la base de datos de Factusol mediante OLEDB.
- **Configuración de Impuestos Dinámica**: Los porcentajes de IVA y Recargo de Equivalencia se configuran desde el servidor (`appsettings.json`) sin tocar el código.
- **Gestión de Pedidos Móvil**: Interfaz optimizada para móviles con búsqueda avanzada de artículos y clientes.
- **Cálculo de Precios Inteligente**: Soporta múltiples tarifas y realiza el desglose automático de bases e impuestos.

## 🛠️ Tecnologías

- **Frontend**: Blazor WebAssembly, MudBlazor (UI).
- **Backend**: ASP.NET Core Minimal APIs.
- **Persistencia**: Integración nativa con Factusol (MS Access/OLEDB).
- **Shared**: Modelos de datos compartidos entre Cliente y API para máxima coherencia.

## ⚙️ Configuración de Impuestos

El sistema utiliza un mapeo dinámico basado en el índice de IVA de Factusol (1, 2, 3). Puedes ajustarlos en `Api/appsettings.json`:

```json
"IvaConfig": {
  "1": { "IVA": 21.0, "RE": 5.2 },
  "2": { "IVA": 10.0, "RE": 1.4 },
  "3": { "IVA": 4.0, "RE": 0.5 }
}
```

## 🚀 Instalación y Uso

1. Configura la ruta de tu base de datos de Factusol en el sistema.
2. Define los impuestos vigentes en `appsettings.json`.
3. Ejecuta el proyecto `Api`.
4. Accede desde tu móvil o navegador para empezar a crear pedidos.

---
Desarrollado con ❤️ para optimizar la gestión comercial.
