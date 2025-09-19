# BlazorTS.Assets

Asset management services for BlazorTS applications with support for dynamic URL generation and deployment flexibility.

## Features

- 🔄 **Dynamic URL Generation**: Support for placeholder-based URL templates
- 🌐 **Reverse Proxy Support**: Handles X-Forwarded-* headers for proxy deployments
- 🚀 **Development/Production**: Unified asset resolution for all environments
- 📁 **Vite Integration**: Built-in support for Vite manifest files
- 🔧 **Zero Configuration**: Works out of the box with sensible defaults

## Installation

```bash
dotnet add package BlazorTS.Assets
```

## Quick Start

### 1. Register Services

```csharp
// Program.cs
builder.Services.AddAssetIntegration(options =>
{
    options.ManifestPath = ".vite/manifest.json";  // Default
    options.DevServerUrl = "http://localhost:5173"; // Default
});
```

### 2. Use in Components

```csharp
@inject IAssetService Assets

<script src="@Assets["main.js"]"></script>
<link rel="stylesheet" href="@Assets["style.css"]">

@if (Assets.ImportMap != null)
{
    <script type="importmap">@Assets.ImportMap</script>
}
```

## Dynamic URL Configuration

### Basic Usage

For simple scenarios, use fixed URLs:

```csharp
builder.Services.AddAssetIntegration(options =>
{
    options.DevServerUrl = "http://localhost:5173";
});
```

### Dynamic URLs with Placeholders

For deployments behind reverse proxies or with dynamic hostnames:

```csharp
builder.Services.AddAssetIntegration(options =>
{
    // Dynamic URL based on current request
    options.DevServerUrl = "{scheme}://{host}{pathBase}";
});
```

### Available Placeholders

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{scheme}` | HTTP/HTTPS protocol | `https` |
| `{host}` | Hostname (without port) | `example.com` |
| `{port}` | Port with colon (empty for 80/443) | `:8080` or `` |
| `{pathBase}` | ASP.NET Core PathBase + proxy prefix | `/app` |

### Configuration Examples

**Development with dynamic host:**
```csharp
options.DevServerUrl = "{scheme}://{host}:5173";
// Result: https://localhost:5173/main.js
```

**Production with CDN:**
```csharp
options.DevServerUrl = "https://cdn.example.com";
// Result: https://cdn.example.com/assets/main-abc123.js
```

**Reverse proxy deployment:**
```csharp
options.DevServerUrl = "{scheme}://{host}{pathBase}";
// Result: https://example.com/app/assets/main-abc123.js
```

**Empty for relative paths (production default):**
```csharp
options.DevServerUrl = "";
// Result: /assets/main-abc123.js
```

## Reverse Proxy Support

BlazorTS.Assets automatically handles common reverse proxy headers:

- `X-Forwarded-Proto`: Override request scheme
- `X-Forwarded-Host`: Override request host
- `X-Forwarded-Port`: Override request port
- `X-Forwarded-Prefix`: Additional path prefix

Example with Nginx:
```nginx
location /app/ {
    proxy_pass http://backend/;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_set_header X-Forwarded-Host $host;
    proxy_set_header X-Forwarded-Prefix /app;
}
```

## Development vs Production

### Development Environment
- Uses `DevServerUrl` directly (with or without placeholders)
- Typically points to Vite dev server: `http://localhost:5173`
- Assets resolved as: `{DevServerUrl}/{assetKey}`

### Production Environment
- Reads Vite manifest file for asset mapping
- If `DevServerUrl` has placeholders, applies dynamic resolution
- If `DevServerUrl` is empty/null, returns relative paths
- Assets resolved from manifest with fingerprinted names

## Configuration Options

```csharp
public class AssetOptions
{
    /// <summary>
    /// Path to Vite manifest file (relative to wwwroot)
    /// </summary>
    public string ManifestPath { get; set; } = ".vite/manifest.json";
    
    /// <summary>
    /// Base URL template for assets. Supports placeholders:
    /// {scheme}, {host}, {port}, {pathBase}
    /// </summary>
    public string DevServerUrl { get; set; } = "http://localhost:5173";
}
```

## Import Maps

BlazorTS.Assets automatically generates ES module import maps in production:

```csharp
@if (Assets.ImportMap != null)
{
    <script type="importmap">
        @Html.Raw(Assets.ImportMap.ToJson())
    </script>
}
```

Import maps respect the same URL resolution as regular assets, ensuring consistency.

## Examples

### Multi-tenant Application
```csharp
// Each tenant gets dynamic URLs based on their subdomain
options.DevServerUrl = "{scheme}://{host}{pathBase}/assets";
```

### Docker with Traefik
```csharp
// Works with Traefik's automatic SSL and host routing
options.DevServerUrl = "{scheme}://{host}";
```

### Kubernetes Ingress
```csharp
// Handles ingress path-based routing
options.DevServerUrl = "{scheme}://{host}{pathBase}";
```

## API Reference

### IAssetService

```csharp
public interface IAssetService
{
    /// <summary>
    /// Get asset URL by key
    /// </summary>
    string this[string key] { get; }
    
    /// <summary>
    /// ES module import map (production only)
    /// </summary>
    ImportMapDefinition? ImportMap { get; }
    
    /// <summary>
    /// Whether running in development environment
    /// </summary>
    bool IsDevelopment { get; }
}