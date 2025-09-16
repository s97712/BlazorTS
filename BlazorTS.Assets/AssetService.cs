using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;

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
        return $"{_options.DevServerUrl}/{assetKey}";
    }

    private string GetProductionAssetUrl(string assetKey)
    {
        if (!_manifestCache.Value.TryGetValue(assetKey, out var manifestEntry))
        {
            throw new KeyNotFoundException($"Asset not found in manifest: {assetKey}");
        }

        return NormalizeAssetPath(manifestEntry.File);
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
                entry => NormalizeAssetPath(entry.Value.File)
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
}