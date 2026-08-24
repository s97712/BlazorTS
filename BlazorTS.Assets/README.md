# BlazorTS.Assets

`BlazorTS.Assets` 提供 Vite Manifest 文件读取、程序集路由和实例缓存。

## 注册

```csharp
builder.Services.AddSingleton<IViteManifestResolver, DefaultViteManifestResolver>();
builder.Services.AddScoped<ViteManifestServiceManager>();
```

## 查询

```csharp
@inject ViteManifestServiceManager ManifestManager

@{
    var entry = ManifestManager.GetEntry("Components/Pages/AppCssImportTest.razor.ts");
}
```

默认 Resolver 使用 `IWebHostEnvironment.WebRootFileProvider` 读取主项目 `.vite/manifest.json` 和类库 `_content/{AssemblyName}/.vite/manifest.json`。Vite 产物由 `obj/vite` 构建并通过 Static Web Assets 注册到 Web 根路径。
