### 模块概览

- **功能简介**：`Core` 模块提供 YuanAPI 的入口插件、子模块自动初始化机制以及统一的日志与版本工具，是整个库的基础核心。
- **模块依赖**：
  - BepInEx（`BepInEx`, `BepInEx.Logging`）
  - Harmony（`HarmonyLib`）
  - 游戏本体相关程序集（包含 `Mainload`, `SaveData`, `SetPanel` 等类型）
- **版本状态**：当前实现已可用于实际模组开发，但仍处于 **开发中/不完全稳定** 状态（例如 `Version` 类构造函数实现存在小问题），后续可能有 API 调整。

### 快速开始

- **引入方式**：
  - 引用 YuanAPI 的 DLL（通常放在 `BepInEx/plugins` 下）。
  - 在你的 BepInEx 插件中添加对命名空间 `YuanAPI` 的引用。
  - 其他模块（如 [本地化模块](Localization.md)、[物品注册器](PropRegistry.md) 等）会通过 `Core` 的子模块系统自动初始化，通常不需要你手动调用 `Initialize`。

- **最小示例**：在 BepInEx 插件中使用日志与版本信息

```csharp
using BepInEx;
using YuanAPI;

[BepInPlugin("com.example.myplugin", "MyPlugin", "1.0.0")]
public class MyPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        // 这里 YuanAPI 自己的插件会在游戏中作为一个独立插件加载
        // 你只需在项目中引用 YuanAPI.dll 即可使用其公共 API

        // 使用 YuanAPI 的日志
        YuanLogger.LogInfo($"MyPlugin 启动，游戏版本：{GameVersion.GetGameVersion()}");

        // 访问 YuanAPI 自身版本信息
        var apiVersion = YuanAPIPlugin.Version;
        YuanLogger.LogInfo($"当前 YuanAPI 版本：{apiVersion}");
    }
}
```

> 提示：`YuanAPIPlugin` 这个类本身是 YuanAPI 的插件入口，不需要在你的插件里继承或创建实例，只要把 YuanAPI 作为依赖 DLL 引入即可。

### 使用注意事项

- **线程安全性**：
  - `YuanLogger` 最终会调用 BepInEx 的 `ManualLogSource`，按照 BepInEx 的常规使用，建议在 Unity 主线程中调用。
  - 与 Unity 对象/游戏状态相关的逻辑（例如通过其他模块访问 `Mainload`、`SaveData` 等）必须在 Unity 主线程调用。
- **生命周期**：
  - `YuanAPIPlugin` 作为 BepInEx 插件，在其 `Awake` 中会调用 `SubmoduleManager.Initialize()`，自动为带有 `[Submodule]` 特性的类打补丁。
  - 标记为 `[Submodule]` 并在其方法上使用 `[AutoInit]` 的模块（如 [Localization](Localization.md)、[PropRegistry](PropRegistry.md)、[ResourceRegistry](ResourceRegistry.md)）在首次调用这些方法时会自动保证自身 `Initialize` 被调用且只调用一次。
  - 你 **无需也不应** 手动调用 `SubmoduleManager.Initialize()` 或 `InitializePatch` 的方法。
- **兼容性提示**：
  - `YuanAPIPlugin.BuildFor` 声明了当前构建针对的游戏版本（`0.7.851`），在使用前建议确认你的游戏版本不低于该版本。
  - 需使用与该版本游戏兼容的 BepInEx 和 Harmony 版本。
- **与其他模组的交互**：
  - `Core` 本身只提供 Harmony 补丁基础，不直接修改游戏逻辑；实际 Harmony 补丁都放在对应模块（如 Localization/PropRegistry 的 `Patches` 命名空间）中。
  - 如果你自己也使用 Harmony，对同一目标方法打补丁时，注意合理设置 `[HarmonyPriority]` 以控制执行顺序。

### API 参考

#### `YuanAPIPlugin` （插件入口）

- **命名空间**：`YuanAPI`
- **类型**：`public class YuanAPIPlugin : BaseUnityPlugin`
- **类说明**：YuanAPI 的 BepInEx 插件入口类。主要负责设置内部日志实现并初始化子模块系统。

- **构造与生命周期**：
  - 由 BepInEx 在加载插件时自动创建，不需要也不应该自行实例化。
  - 关键生命周期方法：
    - `private void Awake()`：设置 `YuanLogger` 的日志实现，调用 `SubmoduleManager.Initialize()`。
    - `private void Start()`：在插件 Start 时触发静态事件 `OnStart`。

- **字段与属性**：
  - `public const string MODNAME`：插件名称，固定为 `"YuanAPI"`。
  - `public const string MODGUID`：插件 GUID，形如 `"cc.lymone.HoL.YuanAPI"`。
  - `public const string VERSION`：当前 YuanAPI 版本号（字符串），例如 `"0.1.1"`。
  - `public static readonly Version BuildFor`：目标游戏版本（SemVer 格式，封装在自定义 `Version` 类型中）。
  - `public static readonly Version Version`：当前 YuanAPI 版本（`VERSION` 解析后的 `Version` 实例）。
  - `internal static Harmony Harmony`：供内部模块共用的 Harmony 实例（不建议外部直接使用）。

- **事件**：
  - `internal static event Action OnStart`：
    - **说明**：在 `Start` 方法中触发，供内部模块（如 Localization、PropRegistry 等）在“游戏开始后”挂载逻辑。
    - **外部使用建议**：该事件是 `internal`，一般模组作者不能直接订阅。

#### `YuanLogger`（静态日志工具）

- **命名空间**：`YuanAPI`
- **类型**：`public static class YuanLogger`
- **类说明**：YuanAPI 内部和上层模块的统一日志入口，包装了一个 `IYuanLogger` 实现。

- **构造与获取方式**：
  - 静态类，无需实例化。

- **方法**：
  - `public static void SetLogger(IYuanLogger logger)`
    - **功能**：为 YuanAPI 设置实际使用的日志实现。
    - **参数**：
      - `logger`：实现了 `IYuanLogger` 的类型实例，一般由 YuanAPI 在自己的插件中用 `LoggerWrapper` 绑定 BepInEx 的 `ManualLogSource`。
    - **使用建议**：**不建议在 YuanAPI 外部调用**，除非你非常清楚日志路由的后果。
  - `LogFatal(object data)` / `LogError(object data)` / `LogWarning(object data)` / `LogMessage(object data)` / `LogInfo(object data)` / `LogDebug(object data)`
    - **功能**：输出不同等级的日志。
    - **参数**：
      - `data`：任意将被格式化输出的对象。
    - **异常**：如果尚未调用 `SetLogger`，可能导致空引用异常（由调用方捕获）。

- **使用示例**：

```csharp
YuanLogger.LogInfo("初始化完成");
YuanLogger.LogWarning($"当前游戏版本：{GameVersion.GetGameVersion()}");
```

#### `IYuanLogger`（日志接口）

- **命名空间**：`YuanAPI`
- **类型**：`public interface IYuanLogger`
- **类说明**：抽象日志接口，为 YuanAPI 与 BepInEx 或单元测试之间提供解耦。

- **方法**：
  - `void LogFatal(object data)`
  - `void LogError(object data)`
  - `void LogWarning(object data)`
  - `void LogMessage(object data)`
  - `void LogInfo(object data)`
  - `void LogDebug(object data)`
  - 各方法语义对应 BepInEx 的同名日志等级。

#### `LoggerWrapper`（日志包装器）

- **命名空间**：`YuanAPI`
- **类型**：`public class LoggerWrapper : IYuanLogger`
- **类说明**：对 BepInEx 的 `ManualLogSource` 的简单包装，实现 `IYuanLogger` 接口。

- **构造方法**：
  - `public LoggerWrapper(ManualLogSource logSource)`
    - **参数**：
      - `logSource`：BepInEx 插件中的 `Logger` 实例。

- **主要方法**：
  - 实现了 `IYuanLogger` 的全部方法，内部直接调用对应的 `logSource.LogXxx`。

#### `Version`（语义化版本类）

- **命名空间**：`YuanAPI`
- **类型**：`public class Version : IComparable<Version>, IEquatable<Version>`
- **类说明**：符合 SemVer 规范的版本号实现，用于描述 YuanAPI 和目标游戏版本。

- **构造方法**：
  - `public Version(int major, int minor, int patch, string? preRelease = null, string? buildMetadata = null)`
  - `public Version(string versionString)`（当前实现逻辑存在问题，建议改用 `Parse`/`TryParse`）

- **静态工厂方法**：
  - `public static Version Parse(string versionString)`
    - 从语义化版本字符串解析 `Version`。
    - 无法解析时抛出 `ArgumentException`。
  - `public static bool TryParse(string versionString, out Version? version)`
    - 尝试解析，失败时返回 `false` 且 `version` 为 `null`。

- **属性**：
  - `int Major`：主版本号。
  - `int Minor`：次版本号。
  - `int Patch`：修订号。
  - `string? PreRelease`：预发布标识，如 `"alpha.1"`。
  - `string? BuildMetadata`：构建元数据，如 `"build.123"`。

- **比较与相等性**：
  - 实现 `==`, `!=`, `<`, `<=`, `>`, `>=` 运算符，比较规则符合 SemVer。
  - `CompareTo` / `Equals` / `GetHashCode` 也相应实现。

- **示例**：

```csharp
var v1 = Version.Parse("1.2.3");
var v2 = Version.Parse("1.2.3-alpha.1");

if (v2 < v1)
{
    YuanLogger.LogInfo($"{v2} 低于 {v1}");
}
```

#### `EnumerableExtension`（枚举扩展方法）

- **命名空间**：`YuanAPI`
- **类型**：`public static class EnumerableExtension`
- **类说明**：为 `IEnumerable<T>` 提供带索引的 `ForEach` 扩展，供内部模块使用。

- **方法**：
  - `public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T, int> action)`
    - **功能**：遍历集合并提供元素索引。
    - **参数**：
      - `enumerable`：要遍历的集合。
      - `action`：回调，参数为 `(item, index)`。

```csharp
var list = new[] { "a", "b", "c" };
list.ForEach((item, index) =>
{
    YuanLogger.LogDebug($"{index}: {item}");
});
```

#### `InitializePatch`（子模块初始化补丁）

- **命名空间**：`YuanAPI`
- **类型**：`public static class InitializePatch`
- **类说明**：Harmony 补丁承载类，负责在调用子模块带有 `[AutoInit]` 特性的方法前确保其 `Initialize` 被调用一次。**通常不需要也不应该被外部代码直接调用**。

- **主要方法**：
  - `public static bool MethodPrefix(MethodBase __originalMethod)`
  - `public static bool InitializePrefix(MethodBase __originalMethod)`
  - 由 `SubmoduleManager` 通过 Harmony 自动挂载，不应主动调用。

### 常见问题（FAQ）

- **Q：我要使用本地化/物品注册等功能，需要手动调用 `Localization.Initialize()` 或 `PropRegistry.Initialize()` 吗？**  
  **A**：不需要。只要调用带有 `[AutoInit]` 的公共方法（例如 `Localization.RegisterLocale`、`PropRegistry.RegisterProps` 等），子模块会通过 `Core` 中的子模块系统自动初始化。

- **Q：能否在自己的插件里修改 `YuanLogger` 的实现？**  
  **A**：技术上可以通过 `YuanLogger.SetLogger` 替换日志实现，但一般不推荐，这可能影响到所有依赖 YuanAPI 的模组。更好的方式是在自己插件内部使用独立日志或在默认日志输出基础上进行处理。

- **Q：`Version` 类如何与 .NET 自带的 `System.Version` 共存？**  
  **A**：两者名称相同、命名空间不同（`YuanAPI.Version` 与 `System.Version`）。建议在代码中使用 `using Version = YuanAPI.Version;` 或写全名来区分。

