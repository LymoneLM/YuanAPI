### 模块概览

- **功能简介**：`Localization` 模块为游戏和模组提供统一的多语言管理能力，包括读取原版本地化文本、注册新语言、加载外部 JSON 语言文件以及按语言/命名空间/键获取文本。
- **模块依赖**：
  - `YuanAPI` 核心模块（日志、子模块系统）
  - 游戏本体本地化结构（如 `AllText`, `Mainload`, `SaveData`, `SetPanel` 等）
  - Harmony（`HarmonyLib`）
  - JSON 解析库 `Newtonsoft.Json`
- **版本状态**：功能较为完备，可在实际模组中稳定使用；后续可能补充更多工具方法和配置项。

### 快速开始

- **引入方式**：
  - 引用 YuanAPI DLL，并在代码中添加 `using YuanAPI;`。
  - `Localization` 被标记为 `[Submodule]`，其 `Initialize` 会通过 `Core` 中的子模块系统和 `[AutoInit]` 自动调用，因此只需直接使用带 `[AutoInit]` 的公共方法（如 `RegisterLocale`、`LoadFromPath` 等），无需手动初始化。

- **最小示例**：添加自定义语言并加载本地 JSON 文本

```csharp
using System.Collections.Generic;
using BepInEx;
using YuanAPI;

[BepInPlugin("com.example.localizationdemo", "Localization Demo", "1.0.0")]
public class LocalizationDemoPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        // 注册一种新语言（例如繁体中文），指定回退到简体中文
        Localization.RegisterLocale("zh-TW", "繁體中文", new List<string> { Localization.DefaultLocale });

        // 从你的插件目录下的 locales 文件夹加载多语言文本
        // 目录结构示例：
        // plugins/MyMod/locales/zh-CN/Common.json
        // plugins/MyMod/locales/zh-TW/Common.json
        var pluginDir = Info.Location; // BepInEx 插件 Assembly 路径
        var modPath = System.IO.Path.GetDirectoryName(pluginDir);
        Localization.LoadFromPath(modPath);

        // 按语言/命名空间/键获取文本
        var hello = Localization.GetText("zh-TW", "Common", "Hello");
        YuanLogger.LogInfo($"Hello in zh-TW: {hello}");

        // 使用实例化后的快捷访问器
        var loc = Localization.CreateInstance(locale: "zh-TW", @namespace: Localization.DefaultNamespace);
        YuanLogger.LogInfo(loc.t("Hello"));
    }
}
```

> 提示：`LoadFromPath` 会在指定路径下查找 `locales/{locale}/{namespace}.json` 结构的文件，并按注册过的所有语言加载。

### 使用注意事项

- **线程安全性**：
  - `Localization` 主要操作字典、列表和文件 IO，自身线程安全性有限；方法内部未使用锁。
  - 推荐在 Unity 主线程或初始化阶段调用，以避免与游戏读写本地化数据同时发生冲突。
- **生命周期**：
  - `Localization.Initialize()` 在模块首个 `[AutoInit]` 方法调用之前自动执行，只会执行一次：
    - 给原版 `AllText` 字段建立内部存储副本。
    - 为 `SetPanel` 和 `SaveData` 挂载 Harmony 补丁。
    - 注册默认语言（`zh-CN`）和英语（`en-US`），并设置 `OnStart` 事件注入回游戏。
  - 你的模组通常只需要：
    1. 在插件 `Awake`/`Start` 中调用 `RegisterLocale`（可选）。
    2. 调用 `LoadFromPath` 加载语言文件。
    3. 使用 `GetText` 或 `LocalizationInstance` 读取文本。
- **兼容性提示**：
  - 要求游戏以 `AllText` 形式存储原版本地化；如果游戏更新导致结构变更，可能需要同步更新 YuanAPI。
  - `LoadFromPath` 使用 `Newtonsoft.Json` 严格解析 JSON，不合法的 JSON 会抛出异常并阻止该文件加载。
- **与其他模组的交互**：
  - 设置语言选项与保存语言索引的补丁通过 `SetPanelPatch` 和 `SaveDataPatch` 实现，可能与修改相同 UI 或存档结构的其他模组交互。
  - 如果你也用 Harmony 修改 `SetPanel` 或 `SaveData`，请注意补丁优先级（`[HarmonyPriority]`）和 Patch 顺序，避免覆盖掉 YuanAPI 的行为。

### API 参考

#### `Localization` 类

- **命名空间**：`YuanAPI`
- **类型**：`[Submodule] public class Localization`
- **类说明**：负责管理所有本地化数据，支持注册新语言、加载 JSON 文本、查询文本以及在游戏原有本地化系统中注入新增/修改的文本。

- **常量**：
  - `public const string DefaultLocale  = "zh-CN"`：默认语言代码。
  - `public const string DefaultNamespace = "Common"`：默认文本命名空间。
  - `public const string VanillaNamespace = "Vanilla"`：用于区分原版文本的数据命名空间。

- **事件**：
  - `public static event Action<string> OnLanguageChanged`
    - **说明**：当语言改变时触发，参数为新的语言代码。
    - **触发时机**：由 `Localization.CallLanguageChanged` 在 `SetPanelPatch` 的 `SetLanguagePostFix` 中调用，当玩家在设置界面切换语言时触发。

##### 初始化与语言注册

- `public static void Initialize()`
  - **功能**：初始化本地化模块，打 Harmony 补丁、读取原版文本并建立内存索引。
  - **调用方式**：通过子模块系统自动调用；不建议手动调用。

- `public static void RegisterLocale(string locale, string showName, List<string> fallbackChain = null)`
  - **特性**：`[AutoInit]`
  - **功能**：注册一种新的语言。
  - **参数**：
    - `locale`：语言代码（如 `"zh-CN"`、`"en-US"`），不能为空。
    - `showName`：在游戏中显示的语言名称。
    - `fallbackChain`：回退链（可选），当该语言缺少某键时按顺序尝试其他语言；默认为 `[DefaultLocale]`。
  - **返回值**：无。
  - **异常**：
    - `ArgumentException`：`locale` 为空或空白字符串。
  - **行为说明**：
    - 重复注册同一语言代码会被忽略并打印警告。
    - 每次注册会清空内部 `_searchOrders` 缓存，下次查找时重新构建回退顺序。

- `public static void SetFallbackChain(string locale, List<string> fallbackChain)`
  - **特性**：`[AutoInit]`
  - **功能**：设置某语言的回退链。
  - **参数**：
    - `locale`：目标语言代码，需要已注册。
    - `fallbackChain`：按优先级排列的回退语言列表。
  - **异常**：
    - `ArgumentException`：`locale` 未注册。
    - 逻辑上如检测到回退链形成环，会在日志中输出错误（不抛异常）。

##### 文本加载与编辑

- `public static void LoadFromPath(string path)`
  - **特性**：`[AutoInit]`
  - **功能**：从指定目录下加载所有已注册语言的 JSON 文本，后读覆盖前读。
  - **参数**：
    - `path`：`locales` 文件夹的父目录路径。
  - **目录约定**：
    - `"{path}/locales/{locale}/{namespace}.json"`
  - **异常**：
    - `DirectoryNotFoundException`：路径不存在或找不到 `locales` 目录。
    - `InvalidDataException`：JSON 根不是对象，或值不是字符串，或 JSON 语法不合法（内部由 `JsonReaderException` 包装）。

- `public static void EditText(string loc, string ns, string key, string value)`
  - **特性**：`[AutoInit]`
  - **功能**：修改/新增某个特定语言、命名空间、键的文本。
  - **参数**：
    - `loc`：语言代码。
    - `ns`：命名空间。
    - `key`：键。
    - `value`：值，`null` 将被转换为空字符串。

- `public static void EditText(string ns, string key, List<string> values)`
  - **功能**：为所有已注册语言批量设置同一命名空间/键的文本列表。
  - **参数**：
    - `ns`：命名空间。
    - `key`：键。
    - `values`：文本列表，按语言注册顺序对应。
  - **行为**：如果 `values` 的长度小于语言数，多出的语言会被忽略（保持原值）。

##### 文本查询

- `public static string GetLocale(int index)`
  - **功能**：根据索引获取语言代码。
  - **参数**：
    - `index`：索引（通常来自 `Mainload.SetData[4]`）。
  - **异常**：索引越界时抛出运行时异常（`ArgumentOutOfRangeException`）。

- `public static string GetText(string locale, string @namespace, string key)`
  - **功能**：按语言/命名空间/键获取文本，支持回退链。
  - **参数**：
    - `locale`：主语言代码。
    - `@namespace`：命名空间。
    - `key`：键；为空时直接返回空字符串。
  - **返回值**：
    - 找到匹配项时返回文本。
    - 未找到时返回 `"{namespace}:{key}"` 并输出警告日志。

- `public static List<string> GetTextAllLocales(string @namespace, string key)`
  - **功能**：返回所有语言下某一命名空间/键的文本。
  - **返回值**：按语言注册顺序排列的文本列表。

- `public static List<string> GetAllLocales()`
  - **功能**：返回当前已注册的语言代码列表（引用内部列表，谨慎修改）。

- `public static List<string> GetAllShowNames()`
  - **功能**：返回所有语言的显示名称列表。

- `public static int LocaleCount()`
  - **功能**：返回已注册语言数。

##### 文本实例访问器

- `public static Localization.LocalizationInstance CreateInstance(string locale = DefaultLocale, string @namespace = DefaultNamespace, bool syncGlobalLocale = true)`
  - **特性**：`[AutoInit]`
  - **功能**：基于给定语言和命名空间创建一个便利的实例访问器，减少参数输入。
  - **参数**：
    - `locale`：默认语言代码。
    - `@namespace`：默认命名空间。
    - `syncGlobalLocale`：是否与全局语言同步（当 `OnLanguageChanged` 触发时自动更新 `Locale` 属性）。

###### `LocalizationInstance` 内部类

- **类型**：`public class LocalizationInstance`
- **属性**：
  - `public string Locale { get; set; }`：当前实例使用的语言代码。
  - `public string Namespace { get; set; }`：当前实例默认命名空间。
  - `public bool IsSyncGlobalLang { get; set; }`：
    - 设置为 `true` 时自动订阅 `Localization.OnLanguageChanged`，当全局语言变更时同步更新 `Locale`。
    - 设置为 `false` 时取消订阅。

- **方法（文本获取）**：
  - `public string t(string key)`
  - `public string t(string @namespace, string key)`
  - `public string t(string locale, string @namespace, string key)`
  - `public string t(string key, params object[] args)`
  - `public string t(string @namespace, string key, params object[] args)`
  - `public string t(string locale, string @namespace, string key, params object[] args)`
    - **说明**：
      - 一系列重载，用于在实例默认或指定语言/命名空间下获取文本，并可选使用 `string.Format` 进行格式化。

- **示例**：

```csharp
var loc = Localization.CreateInstance(locale: "en-US", @namespace: "MyMod");

// 简单获取
var title = loc.t("UI.Title");

// 指定命名空间
var btnText = loc.t("Buttons", "Confirm");

// 带格式化参数
var msg = loc.t("Messages", "HelloUser", "Player1");
```

### 常见问题（FAQ）

- **Q：JSON 语言文件的结构有什么要求？**  
  **A**：根节点必须是对象，所有叶子节点必须是字符串。嵌套对象会被用“`.`”拼接为键，例如：

```json
{
  "UI": {
    "Title": "标题",
    "Confirm": "确定"
  }
}
```

会生成键 `UI.Title` 和 `UI.Confirm`。

- **Q：如何自定义语言文件路径？**  
  **A**：`LoadFromPath(path)` 参数为 `locales` 的父目录。如果你的结构是 `BepInEx/plugins/MyMod/locales/...`，可传入 `MyMod` 目录路径即可。

- **Q：如何在多线程环境下安全使用 `Localization`？**  
  **A**：建议在主线程内完成语言文件加载与修改；读取操作虽主要为字典查找，但同时写入（如 `EditText`）可能产生竞态条件。高并发使用时请自行在调用层加锁。

- **Q：我已经在游戏原有 `AllText` 上做了修改，YuanAPI 的注入会不会覆盖？**  
  **A**：`Localization` 在注入时会基于内部 `_store` 重构 `AllText` 内容，如果你在 YuanAPI 初始化之后再直接改写 `AllText`，后续可能被注入覆盖。推荐通过 `Localization.EditText` 或在 YuanAPI 加载前修改，以保持一致性。

