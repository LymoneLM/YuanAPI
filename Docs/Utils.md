### 模块概览

- **功能简介**：`Utils` 模块包含 YuanAPI 内部使用的辅助类型，例如日志封装、语义化版本号解析以及集合扩展方法。虽然多数设计为内部工具，但对模组作者也可以直接复用。
- **模块依赖**：
  - .NET 基础库（`System`, `System.Text.RegularExpressions`, `System.Collections.Generic` 等）
  - 游戏本体（`Mainload`，用于 `GameVersion` 内部获取游戏版本）
- **版本状态**：工具类相对稳定，但**不保证长期 API 不变**，未来可能根据内部需求进行调整。

### 快速开始

- **引入方式**：
  - 引用 YuanAPI DLL，在代码中添加 `using YuanAPI;`。
  - 推荐直接使用：
    - `YuanLogger` / `LoggerWrapper` / `IYuanLogger`：统一日志。
    - `Version`：按 SemVer 规则解析与比较版本。
    - `EnumerableExtension.ForEach`：简化集合遍历。

- **最小示例**：使用版本工具与扩展方法

```csharp
using System.Collections.Generic;
using BepInEx;
using YuanAPI;

[BepInPlugin("com.example.utilsdemo", "Utils Demo", "1.0.0")]
public class UtilsDemoPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        // 使用语义化版本判断
        var apiVersion = YuanAPIPlugin.Version; // YuanAPI 自身版本（YuanAPI.Version 类型）
        var minRequired = Version.Parse("0.1.0");
        if (apiVersion < minRequired)
        {
            Logger.LogWarning($"当前 YuanAPI 版本 {apiVersion} 低于推荐版本 {minRequired}");
        }

        // 使用 ForEach 扩展
        var list = new List<string> { "A", "B", "C" };
        list.ForEach((item, index) =>
        {
            YuanLogger.LogDebug($"Item {index}: {item}");
        });
    }
}
```

### 使用注意事项

- **线程安全性**：
  - `YuanLogger` 的线程安全性取决于内部日志实现（默认是 BepInEx 的 `ManualLogSource`，一般在主线程使用）。
  - `Version` 与 `EnumerableExtension` 仅操作托管对象，本身线程安全，但仍建议不要在复杂并发场景下共享可变集合。
- **生命周期**：
  - `GameVersion.GetGameVersion()` 依赖 `Mainload.Vision_now`，需要在游戏版本信息已可用时调用。
  - `YuanLogger` 必须在 `YuanAPIPlugin.Awake` 中完成 `SetLogger` 绑定后再使用，否则可能触发空引用异常。
- **兼容性提示**：
  - `Version` 类与 `System.Version` 重名，使用时注意命名空间区分（`YuanAPI.Version`）。
  - `Version` 构造函数 `new Version(string)` 当前实现存在逻辑问题，建议优先使用 `Version.Parse`/`TryParse`。
- **与其他模组的交互**：
  - 如果你自行调用 `YuanLogger.SetLogger` 更换日志实现，可能影响所有依赖 YuanAPI 的模组，务必谨慎。

### API 参考

> 其中部分类型已在 [Core 模块文档](Core.md) 中详细说明，这里只对 `Utils` 目录下的核心工具进行补充。

#### `GameVersion`（游戏版本获取，内部使用）

- **命名空间**：`YuanAPI`
- **类型**：`internal static class GameVersion`
- **说明**：按项目设计仅供内部使用，非 public 类型，此处仅简单说明其行为：
  - `public static Version GetGameVersion()`
    - 从 `Mainload.Vision_now` 的字符串中截去前两位字符，然后尝试解析为 `YuanAPI.Version`。
    - 若长度不够或解析失败，返回 `null`。
  - 一般情况下你可以通过游戏自身 API 或自定义逻辑获取版本，而无需直接依赖此类。

#### `EnumerableExtension`（集合扩展）

- **命名空间**：`YuanAPI`
- **类型**：`public static class EnumerableExtension`
- **类说明**：为 `IEnumerable<T>` 增加一个带索引的 `ForEach` 扩展方法。

- **方法**：
  - `public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T, int> action)`
    - **功能**：遍历集合并对每个元素执行回调，回调参数中包含元素和其索引。
    - **参数**：
      - `enumerable`：要遍历的集合。
      - `action`：回调，形如 `(item, index) => { ... }`。
    - **异常**：
      - 如果 `action` 为 `null` 会抛出 `NullReferenceException`（由调用方负责避免）。

- **使用示例**：

```csharp
var numbers = new[] { 10, 20, 30 };
numbers.ForEach((value, index) =>
{
    YuanLogger.LogInfo($"Index {index}, Value {value}");
});
```

#### `Version`（语义化版本）

> 详见 [Core 模块文档](Core.md) 中的 `Version` 章节，这里只补充与 `Utils` 定位相关的说明。

- **典型用途**：
  - 限定模组依赖的最小 YuanAPI 版本/游戏版本。
  - 在插件启动时进行版本检查并向玩家输出友好提示。

- **示例**：

```csharp
var gameVersion = GameVersion.GetGameVersion();
var minVersion = Version.Parse("0.7.851");

if (gameVersion != null && gameVersion < minVersion)
{
    YuanLogger.LogWarning($"当前游戏版本 {gameVersion} 低于推荐版本 {minVersion}");
}
```

#### 日志工具总览（`IYuanLogger` / `LoggerWrapper` / `YuanLogger`）

- 已在 [Core 模块文档](Core.md) 的“YuanLogger / IYuanLogger / LoggerWrapper”部分详细说明，这里仅总结用途：
  - **`IYuanLogger`**：抽象日志接口，方便替换实现或在单元测试中注入模拟对象。
  - **`LoggerWrapper`**：对 BepInEx `ManualLogSource` 的包装，将其适配为 `IYuanLogger`。
  - **`YuanLogger`**：静态日志入口，供 YuanAPI 内部以及模组作者调用。

### 常见问题（FAQ）

- **Q：我可以直接使用 `GameVersion.GetGameVersion()` 吗？**  
  **A**：可以，但需要注意它是 `internal` 类型，一般在你的项目中无法直接访问。如果需要获取游戏版本，建议通过游戏自身公开 API 或在你自己的模组中实现相似逻辑。

- **Q：`Version` 与 `System.Version` 同名如何避免冲突？**  
  **A**：可以：
  - 使用 `using Version = YuanAPI.Version;` 起别名；或
  - 在代码中显式写 `YuanAPI.Version` 来引用。

- **Q：`EnumerableExtension.ForEach` 与 LINQ 的 `ForEach` 有什么区别？**  
  **A**：.NET 标准库中只有 `List<T>.ForEach`，不直接提供 `IEnumerable<T>.ForEach`。本扩展可以对任何实现 `IEnumerable<T>` 的集合使用，并且提供索引参数。

