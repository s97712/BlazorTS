using System.Text.Json.Serialization;

namespace BlazorTS.Assets;

/// <summary>
/// Represents an entry in the asset manifest.
/// </summary>
public class AssetManifestEntry
{
    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [JsonPropertyName("src")]
    public string? Src { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this is an entry point.
    /// </summary>
    [JsonPropertyName("isEntry")]
    public bool? IsEntry { get; set; }
    
    /// <summary>
    /// Gets or sets the CSS files associated with this entry.
    /// </summary>
    [JsonPropertyName("css")]
    public string[]? Css { get; set; }
}