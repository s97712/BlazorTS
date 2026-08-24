using System.Text.Json.Serialization;

namespace BlazorTS.Assets;

/// <summary>
/// 表示 Vite 构建清单中的单个资产条目。
/// </summary>
public class ViteManifestEntry
{
    /// <summary>
    /// 输出文件相对路径。
    /// </summary>
    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    /// <summary>
    /// 源文件路径。
    /// </summary>
    [JsonPropertyName("src")]
    public string? Src { get; set; }

    /// <summary>
    /// 是否为入口文件。
    /// </summary>
    [JsonPropertyName("isEntry")]
    public bool? IsEntry { get; set; }

    /// <summary>
    /// 关联的 CSS 样式文件列表。
    /// </summary>
    [JsonPropertyName("css")]
    public string[]? Css { get; set; }

    /// <summary>
    /// 关联的静态资源列表。
    /// </summary>
    [JsonPropertyName("assets")]
    public string[]? Assets { get; set; }

    /// <summary>
    /// 静态导入的模块列表。
    /// </summary>
    [JsonPropertyName("imports")]
    public string[]? Imports { get; set; }

    /// <summary>
    /// 动态导入的模块列表。
    /// </summary>
    [JsonPropertyName("dynamicImports")]
    public string[]? DynamicImports { get; set; }
}
