using Microsoft.AspNetCore.Components;

namespace BlazorTS.Assets;

/// <summary>
/// Defines the contract for a service that provides access to assets.
/// </summary>
public interface IAssetService
{
    /// <summary>
    /// Gets the URL for the specified asset key.
    /// </summary>
    /// <param name="key">The asset key.</param>
    /// <returns>The URL of the asset.</returns>
    string this[string key] { get; }

    /// <summary>
    /// Gets the import map.
    /// </summary>
    ImportMapDefinition? ImportMap { get; }

    /// <summary>
    /// Gets a value indicating whether the application is in the development environment.
    /// </summary>
    bool IsDevelopment { get; }
}