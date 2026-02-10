### 模块概览

- **功能简介**：`ResourceRegistry` 模块统一管理模组资源（预制体、图片、音频等）的加载，通过注册 `ResourceData` 并拦截 `Resources.Load` 调用，把带特定关键字的路径重定向到模组的 AssetBundle 中。
- **模块依赖**：
  - `YuanAPI` 核心模块（日志、子模块系统）
  - Unity 引擎（`UnityEngine`, `Resources`, `AssetBundle`, `AudioClip`, `Sprite` 等）
  - Harmony（`HarmonyLib`）
- **版本状态**：核心功能稳定，可在生产环境中使用；接口来自 CommonAPI，兼容性较好。

### 快速开始

- **引入方式**：
  - 引用 YuanAPI DLL，并在代码中添加 `using YuanAPI;`。
  - 在你的插件中构造一个 `ResourceData`，加载 AssetBundle，并通过 `ResourceRegistry.AddResource` 注册。
  - 在游戏/模组代码中照常使用 `Resources.Load("YourKeyword/Path")`，YuanAPI 会自动从已注册的 AssetBundle 中查找并返回资源。

- **最小示例**：注册 AssetBundle 并从中加载预制体

```csharp
using BepInEx;
using UnityEngine;
using YuanAPI;

[BepInPlugin("com.example.resourcedemo", "Resource Demo", "1.0.0")]
public class ResourceDemoPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        // 1. 构造 ResourceData（路径自动取调用程序集所在目录）
        var resource = new ResourceData("com.example.resourcedemo", "MyMod", modPath: null);

        // 2. 从 mod 目录加载 AssetBundle（例如 MyModAssets）
        //    这里假设 bundle 文件放在插件 DLL 同级目录
        resource.LoadAssetBundle("MyModAssets");

        // 3. 注册资源定义
        ResourceRegistry.AddResource(resource);

        // 4. 在任意位置使用 Resources.Load 加载资源：
        //    如果路径中包含 keyword（这里为 "MyMod"），会从已注册的 AssetBundle 中查找
        var prefab = Resources.Load<GameObject>("MyMod/Prefabs/MyFirstProp");
        if (prefab != null)
        {
            YuanLogger.LogInfo("成功从 AssetBundle 加载 MyFirstProp");
        }
    }
}
```

> 提示：`ResourceData` 的 `KeyWord` 用于匹配 `Resources.Load` 的路径中是否“包含”该字符串，而不仅仅是前缀匹配。

### 使用注意事项

- **线程安全性**：
  - `ModResources` 列表在运行时被 Harmony 补丁遍历，建议只在初始化阶段（主线程）调用 `AddResource`。
  - Unity 的 `AssetBundle.LoadFromFile` 和 `Resources.Load` 都要求在主线程调用。
- **生命周期**：
  - `ResourceRegistry.Initialize()` 会在模块首次使用前通过子模块系统自动调用。
  - 你应在 BepInEx 插件的 `Awake` / `Start` 中调用 `AddResource` 并 `LoadAssetBundle`，确保在游戏逻辑开始依赖这些资源前完成注册。
- **兼容性提示**：
  - AssetBundle 必须使用与目标游戏一致的 Unity 版本构建。
  - 如果多个模组对同一路径使用相同关键字并注册了多个 `ResourceData`，会按照 `ModResources` 列表顺序进行匹配；目前实现会在第一个命中的资源上返回结果。
- **与其他模组的交互**：
  - `ResourceRegistryPatches.ResourcesPatch` 使用 `[HarmonyPriority(Priority.Last)]` 前缀补丁拦截 `Resources.Load`。如果其他模组也对 `Resources.Load` 打补丁，执行顺序将影响最终行为。
  - 如果你需要在自定义补丁中控制与 YuanAPI 的加载顺序，可通过调整自身补丁的 `HarmonyPriority`。

### API 参考

#### `ResourceRegistry` 类

- **命名空间**：`YuanAPI`
- **类型**：`[Submodule] public class ResourceRegistry`
- **类说明**：维护所有模组注册的资源定义，并通过 Harmony 补丁在 `Resources.Load` 执行时重定向到 AssetBundle。

- **字段**：
  - `internal static List<ResourceData> ModResources = []`
    - 当前注册的资源定义列表。
  - `internal static string[] SpriteFileExtensions = [".jpg", ".png", ".tif"]`
  - `internal static string[] AudioClipFileExtensions = [".mp3", ".ogg", ".waw", ".aif"]`

- **方法**：
  - `public static void Initialize()`
    - **功能**：使用 YuanAPI 共享的 `Harmony` 实例为 `ResourceRegistryPatches.ResourcesPatch` 挂载所有补丁。
    - **调用方式**：通过子模块系统自动调用。
  - `public static void AddResource(ResourceData resource)`
    - **特性**：`[AutoInit]`
    - **功能**：向资源注册表中添加一个资源定义。
    - **参数**：
      - `resource`：`ResourceData` 实例。

#### `ResourceException` 异常

- **命名空间**：`YuanAPI`
- **类型**：`public class ResourceException : Exception`
- **类说明**：在资源加载失败时抛出的自定义异常。

- **构造方法**：
  - `public ResourceException(string message)`
    - **参数**：
      - `message`：错误描述。

#### `ResourceData` 类

- **命名空间**：`YuanAPI`
- **类型**：`public class ResourceData`
- **类说明**：描述一个模组资源定义，包括模组 ID、关键字、模块路径以及关联的 AssetBundle。

- **字段**：
  - `public string ModId;`
    - 模组 ID，用于标识资源来自哪个模组。
  - `public string KeyWord;`
    - 用于匹配 `Resources.Load` 路径的关键字。
  - `public string ModPath;`
    - 模组主程序集所在目录路径。
  - `public AssetBundle Bundle;`
    - 已加载的 AssetBundle。

- **构造方法**：
  - `public ResourceData(string modId, string keyWord, string modPath)`
    - **参数**：
      - `modId`：模组 ID。
      - `keyWord`：关键字。
      - `modPath`：模组主程序集目录路径。
  - `public ResourceData(string modId, string keyWord)`
    - **功能**：自动从调用方的程序集位置推断 `ModPath`。
    - **参数**：
      - `modId`：模组 ID。
      - `keyWord`：关键字。

- **方法**：
  - `public bool HasAssetBundle()`
    - **功能**：判断是否已成功加载 AssetBundle。
  - `public void LoadAssetBundle(string bundleName)`
    - **功能**：从 `ModPath` 目录下加载指定名称的 AssetBundle 文件。
    - **参数**：
      - `bundleName`：AssetBundle 文件名。
    - **异常**：
      - `ResourceException`：如果 `AssetBundle.LoadFromFile` 返回 `null`，表示加载失败。

- **示例**：

```csharp
var res = new ResourceData("com.example.mod", "Example");
res.LoadAssetBundle("example_assets");
ResourceRegistry.AddResource(res);
```

#### Harmony 补丁类（高级用法）

- **命名空间**：`YuanAPI.ResourceRegistryPatches`
- **类型**：`public static class ResourcesPatch`
- **类说明**：拦截 `Resources.Load(string, Type)` 调用，对路径中包含任意已注册 `ResourceData.KeyWord` 的资源，尝试从对应 AssetBundle 中加载。

- **核心逻辑**：
  - 对每个 `ModResources` 中的 `resource`：
    - 如果 `path` 不包含 `resource.KeyWord` 或 `resource.HasAssetBundle()` 返回 `false`，跳过。
    - 依次尝试：
      - `resource.Bundle.LoadAsset(path + ".prefab")`。
      - 为每个 `SpriteFileExtensions` 中的扩展名尝试 `LoadAsset(path + extension, systemTypeInstance)`。
      - 为每个 `AudioClipFileExtensions` 中的扩展名尝试 `LoadAsset(path + extension, systemTypeInstance)`。
    - 一旦成功加载，记录调试日志并返回 `false` 阻止原始 `Resources.Load` 执行。

### 常见问题（FAQ）

- **Q：一个路径可以同时被多个 `ResourceData` 匹配到吗？**  
  **A**：是的，如果多个 `KeyWord` 都被包含在同一个路径中，就会被多次检查。根据当前实现，会以 `ModResources` 列表中**先匹配成功**的资源为准。

- **Q：如何只复用 `ResourceData` 而不启用 Harmony 补丁？**  
  **A**：目前 `ResourceRegistry.Initialize` 总是会为 `ResourcesPatch` 打补丁。如果你只想手动使用 `AssetBundle` 而不依赖自动重定向，可以不调用 `AddResource`，而在自己代码中直接使用 `AssetBundle.LoadAsset`。

- **Q：`bundleName` 必须带扩展名吗？**  
  **A**：`LoadAssetBundle` 只是将 `bundleName` 拼接到 `"{ModPath}/{bundleName}"`，是否包含扩展名由你自己决定，只要路径与实际文件匹配即可。

- **Q：如何调试资源加载失败的问题？**  
  **A**：可以：
  - 确认 `ModPath` 和 `bundleName` 路径正确。
  - 检查 `Resources.Load` 使用的路径是否包含你设置的 `KeyWord`。
  - 开启 BepInEx 日志等级（或通过 `YuanLogger.LogDebug`）查看 `"Loading registered asset {path}: Success/Failure"` 的调试信息。

