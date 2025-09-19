using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Linq;

namespace BlazorTS.Assets;

/// <summary>
/// Service for managing assets, handling asset path resolution in development and production environments.
/// </summary>
public class AssetService : IAssetService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AssetOptions _options;
    private readonly Lazy<Dictionary<string, AssetManifestEntry>> _manifestCache;
    private readonly Lazy<ImportMapDefinition?> _importMapCache;

    /// <summary>
    /// Gets a value indicating whether the current environment is development.
    /// </summary>
    public bool IsDevelopment => _environment.IsDevelopment();

    /// <summary>
    /// Gets the import map definition (only available in production).
    /// </summary>
    public ImportMapDefinition? ImportMap => IsDevelopment ? null : _importMapCache.Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetService"/> class.
    /// </summary>
    /// <param name="environment">The web host environment.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <param name="options">The asset options.</param>
    public AssetService(
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        IOptions<AssetOptions> options)
    {
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
        
        // Lazily load the manifest and import map to improve startup performance.
        _manifestCache = new Lazy<Dictionary<string, AssetManifestEntry>>(LoadManifest);
        _importMapCache = new Lazy<ImportMapDefinition?>(CreateImportMap);
    }

    /// <summary>
    /// Gets the URL path for the specified asset key.
    /// </summary>
    /// <param name="assetKey">The asset key.</param>
    /// <returns>The full URL path of the asset.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the asset is not found in the manifest in a production environment.</exception>
    public string this[string assetKey]
    {
        get
        {
            return IsDevelopment
                ? GetDevelopmentAssetUrl(assetKey)
                : GetProductionAssetUrl(assetKey);
        }
    }

    private string GetDevelopmentAssetUrl(string assetKey)
    {
        return ResolveUrl(_options.DevServerUrl, assetKey);
    }

    private string GetProductionAssetUrl(string assetKey)
    {
        if (!_manifestCache.Value.TryGetValue(assetKey, out var manifestEntry))
        {
            throw new KeyNotFoundException($"Asset not found in manifest: {assetKey}");
        }

        return ResolveUrl(_options.DevServerUrl ?? "", manifestEntry.File);
    }

    private static string NormalizeAssetPath(string filePath)
    {
        return filePath.StartsWith('/') ? filePath : $"/{filePath}";
    }

    private Dictionary<string, AssetManifestEntry> LoadManifest()
    {
        if (IsDevelopment)
        {
            return new Dictionary<string, AssetManifestEntry>();
        }

        var manifestPath = Path.Combine(_environment.WebRootPath, _options.ManifestPath);
        
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"Vite manifest file not found: {manifestPath}");
        }

        var manifestJson = File.ReadAllText(manifestPath);
        var deserializeOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<Dictionary<string, AssetManifestEntry>>(manifestJson, deserializeOptions)
               ?? new Dictionary<string, AssetManifestEntry>();
    }

    private ImportMapDefinition? CreateImportMap()
    {
        if (IsDevelopment)
        {
            return null;
        }

        var manifestImports = ExtractManifestImports();
        var endpointImports = GetEndpointImportMap();

        if (manifestImports.Any() || endpointImports is not null)
        {
            return CombineImportMaps(manifestImports, endpointImports);
        }

        return null;
    }

    private Dictionary<string, string> ExtractManifestImports()
    {
        return _manifestCache.Value
            .Where(entry => entry.Value.IsEntry == true && !string.IsNullOrEmpty(entry.Value.Src))
            .ToDictionary(
                entry => entry.Value.Src!,
                entry => ResolveUrl(_options.DevServerUrl ?? "", entry.Value.File)
            );
    }

    private ImportMapDefinition? GetEndpointImportMap()
    {
        return _httpContextAccessor.HttpContext?
            .GetEndpoint()?
            .Metadata
            .GetMetadata<ImportMapDefinition>();
    }

    private static ImportMapDefinition CombineImportMaps(
        Dictionary<string, string> manifestImports,
        ImportMapDefinition? endpointImports)
    {
        var manifestImportMap = new ImportMapDefinition(manifestImports, null, null);
        var fallbackImportMap = endpointImports ?? new ImportMapDefinition(null, null, null);

        return ImportMapDefinition.Combine(manifestImportMap, fallbackImportMap);
    }

    private string ResolveUrl(string template, string assetPath)
    {
        assetPath = NormalizeAssetPath(assetPath);
        
        if (string.IsNullOrEmpty(template))
            return assetPath;
            
        return ApplyTemplate(template) + assetPath;
    }

    private string ApplyTemplate(string template)
    {
        var ctx = _httpContextAccessor.HttpContext;
        var req = ctx?.Request;

        var scheme = req?.Headers["X-Forwarded-Proto"].FirstOrDefault()
                     ?? req?.Scheme
                     ?? "http";

        var hostHeader = req?.Headers["X-Forwarded-Host"].FirstOrDefault()
                         ?? req?.Host.Value
                         ?? "localhost";

        var hostOnly = hostHeader.Contains(':')
            ? hostHeader.Split(':')[0]
            : hostHeader;

        int? portNumber = null;
        var forwardedPort = req?.Headers["X-Forwarded-Port"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedPort) && int.TryParse(forwardedPort, out var fp))
        {
            portNumber = fp;
        }
        else if (req?.Host.Port is int p)
        {
            portNumber = p;
        }

        var port = "";
        if (portNumber.HasValue)
        {
            var isDefault =
                (scheme == "http" && portNumber.Value == 80) ||
                (scheme == "https" && portNumber.Value == 443);

            port = isDefault ? "" : $":{portNumber.Value}";
        }

        var pathBase = req?.PathBase.HasValue == true ? req.PathBase.Value! : "";

        var forwardedPrefix = req?.Headers["X-Forwarded-Prefix"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedPrefix))
        {
            if (!forwardedPrefix.StartsWith("/"))
            {
                forwardedPrefix = "/" + forwardedPrefix;
            }
            pathBase = forwardedPrefix + pathBase;
        }

        return template
            .Replace("{scheme}", scheme)
            .Replace("{host}", hostOnly)
            .Replace("{port}", port)
            .Replace("{pathBase}", pathBase);
    }
}