namespace BlazorTS.Assets;

/// <summary>
/// Asset service configuration options
/// </summary>
public class AssetOptions
{
    /// <summary>
    /// Gets or sets the manifest file path
    /// </summary>
    public string ManifestPath { get; set; } = ".vite/manifest.json";
    
    /// <summary>
    /// Gets or sets the development server URL
    /// </summary>
    public string DevServerUrl { get; set; } = "http://localhost:5173";
}