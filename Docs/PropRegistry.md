### 模块概览

- **功能简介**：`PropRegistry` 模块负责统一管理游戏物品数据（Prop），支持为原版物品建立 UID、注册自定义物品、修改原版物品，并与本地化和资源系统联动。
- **模块依赖**：
  - `YuanAPI` 核心模块（日志、子模块系统）
  - [本地化模块](Localization.md)（用于物品名称/描述文本）
  - [资源注册器模块](ResourceRegistry.md)（用于物品预制体与资源加载）
  - 游戏本体类型：`Mainload`, `AllText`, `SaveData`, `ES3`, `Resources` 等
  - Harmony（`HarmonyLib`）
- **版本状态**：功能较为完整，适合在实际模组中使用；物品数据结构一旦确定，不建议频繁变更字段名。

### 快速开始

- **引入方式**：
  - 引用 YuanAPI DLL，并在代码中添加 `using YuanAPI;`。
  - `PropRegistry` 被标记为 `[Submodule]`，其 `Initialize` 通过子模块系统和 `[AutoInit]` 自动调用，因此你只需直接调用 `RegisterProps` 或 `CreateInstance` 即可。

- **最小示例**：注册一个新物品并在游戏中添加

```csharp
using System.Collections.Generic;
using BepInEx;
using YuanAPI;
using YuanAPI.Tools; // 使用 PropTool 添加物品到背包

[BepInPlugin("com.example.propdemo", "Prop Demo", "1.0.0")]
public class PropDemoPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        // 1. 准备本地化文本（建议在 Localization 中加载 JSON，这里仅示意）
        Localization.EditText("MyMod", "Props.MyFirstProp.Name", "测试物品");

        // 2. 构建物品数据
        var newProp = new PropData
        {
            PropNamespace = "MyMod",
            PropID = "MyFirstProp",
            Price = 100,
            Category = (int)PropCategory.Snack,
            TextNamespace = "MyMod",
            TextKey = "Props.MyFirstProp.Name",
            PrefabPath = "MyModAssets/MyFirstProp" // 对应资源包内路径
        };

        // 3. 注册物品
        PropRegistry.RegisterProps(new List<PropData> { newProp });

        // 4. 在玩家背包中添加该物品（需要等主存档加载完成）
        //    一般建议在游戏进入存档后再调用，例如在某个事件回调或按钮中触发
        PropTool.AddProp(PropRegistry.GetIndex("MyMod", "MyFirstProp"), 1);
    }
}
```

> 提示：物品的本地化文本和预制体路径需要与 [Localization](Localization.md) 和 [ResourceRegistry](ResourceRegistry.md) 中的配置保持一致。

### 使用注意事项

- **线程安全性**：
  - `PropRegistry` 内部维护 `_allProps` 列表与 `_uidMap` 字典，并与 `Mainload.AllPropdata`, `AllText.Text_AllProp` 以及存档数据进行读写。
  - 所有操作应在 Unity 主线程中执行，特别是与 `Mainload`、`AllText`、`ES3` 和 `Resources` 相关的部分。
- **生命周期**：
  - `PropRegistry.Initialize()` 会：
    - 先初始化 [ResourceRegistry](ResourceRegistry.md) 与 [Localization](Localization.md)。
    - 为 `Resources.Load` 和 `SaveData` 打 Harmony 补丁。
    - 在 `YuanAPIPlugin.OnStart` 中注册 `InjectMainload`，在主加载完成后整合原版与自定义物品。
  - 你只需在插件初始化阶段调用 `RegisterProps` / `CreateInstance` 即可；`InjectMainload` 会在游戏开始时统一完成注入。
- **兼容性提示**：
  - 修改原版物品（`PropNamespace == "Vanilla"`）时要谨慎，可能影响其他依赖这些物品的模组或游戏逻辑。
  - 存档格式被改为使用 UID 保存非原版物品 ID，如果卸载某个模组，存档中对应物品 UID 可能无法解析，会被记录错误日志并跳过。
- **与其他模组的交互**：
  - `PropRegistryPatches.ResourcesPatch` 会拦截 `Resources.Load("AllProp/{id}")` 来重定向到自定义预制体。
  - `PropRegistryPatches.SaveDataPatch` 会在存档读写时用 UID 替换/还原物品 ID。
  - 如果其他模组也对 `Resources.Load` 或 `SaveData.SaveGameData` / `ReadGameData` 打补丁，请注意优先级与补丁顺序，避免互相覆盖。

### API 参考

#### `PropRegistry` 类

- **命名空间**：`YuanAPI`
- **类型**：`[Submodule] public class PropRegistry`
- **类说明**：统一管理物品数据与 UID 映射，负责注册新物品、修改原版物品以及在游戏数据结构中注入/还原物品信息。

- **常量**：
  - `public const string DefaultNamespace = "Common"`
  - `public const string VanillaNamespace = "Vanilla"`

- **属性**：
  - `internal static int VanillaPropCount { get; private set; }`
    - 原版物品数量，对外只读。

##### 初始化

- `public static void Initialize()`
  - **功能**：初始化物品注册器，挂载资源和存档相关补丁，并将注入逻辑注册到 `YuanAPIPlugin.OnStart`。
  - **调用方式**：通过子模块系统自动调用；不建议手动调用。

##### 物品访问

- `public static PropData GetProp(string @namespace, string id)`
  - **功能**：通过命名空间与 ID 获取物品数据。
  - **参数**：
    - `@namespace`：物品命名空间。
    - `id`：物品 ID。
  - **返回值**：对应的 `PropData`，若映射不存在会抛出 `KeyNotFoundException`。

- `public static PropData GetProp(string uid)`
  - **功能**：通过 UID（形如 `"namespace:id"`）获取物品数据。

- `public static PropData GetProp(int index)`
  - **功能**：通过内部索引（与 `Mainload.AllPropdata` 相同）获取物品数据。

- `public static string GetUid(int index)`
  - **功能**：通过内部索引获取 UID。

- `public static bool TryGetUid(int index, out string uid)`
  - **功能**：安全地通过索引获取 UID；超出范围时返回 `false`，`uid` 仍会被赋值为格式化字符串，但调用者应根据返回值判断是否有效。

- `public static int GetIndex(string @namespace, string id)`
  - **功能**：通过命名空间与 ID 获取索引。

- `public static bool TryGetIndex(string @namespace, string id, out int index)`
  - **功能**：安全获取索引，不存在时返回 `false`。

- `public static bool TryGetIndex(string uid, out int index)`
  - **功能**：通过 UID 获取索引；UID 不合法或不存在时返回 `false`。

##### 物品注册

- `public static void RegisterProps(List<PropData> props)`
  - **特性**：`[AutoInit]`
  - **功能**：注册一批物品数据。
  - **参数**：
    - `props`：物品数据列表。
  - **行为**：
    - 对于 `PropNamespace == VanillaNamespace` 的数据，认为是对原版物品的修改，延后在 `InjectMainload` 中应用到原版数据。
    - 对于其他命名空间：
      - 已存在相同 `PropNamespace:PropID` 的，**后注册覆盖先注册**。
      - 不存在则新增。
    - `PropData.IsValid()` 返回 `false` 的条目会被跳过并输出错误日志。

##### 物品批量注册实例

- `public static PropRegistry.PropRegistryInstance CreateInstance(PropData sample = null, List<PropData> propList = null)`
  - **特性**：`[AutoInit]`
  - **功能**：创建一个便于小批量注册物品的临时实例，支持基于样例 `PropData` 做差异合并。
  - **参数**：
    - `sample`：样例物品（可选），后续添加的物品会和样例做“+ 运算”以填充默认字段。
    - `propList`：初始物品集合（可选），会立即按 `Add` 逻辑加入。

###### `PropRegistryInstance` 内部类

- **类型**：`public class PropRegistryInstance : IDisposable`
- **属性**：
  - `public PropData Sample { get; set; }`：基础模板物品。

- **方法**：
  - `public void Add(PropData prop)`
    - **功能**：以 `Sample + prop` 的结果添加物品到内部列表。
  - `public void Dispose()`
    - **功能**：在释放时将内部收集的所有物品一次性注册到 `PropRegistry`。
    - **注意**：多次调用 `Dispose` 只有第一次有效，其后会被忽略。

- **示例**：

```csharp
using (var reg = PropRegistry.CreateInstance(
           sample: new PropData
           {
               PropNamespace = "MyMod",
               Price = 100,
               Category = (int)PropCategory.Snack,
               TextNamespace = "MyMod",
               PrefabPath = "MyModAssets/BaseSnack"
           }))
{
    reg.Add(new PropData
    {
        PropID = "Snack1",
        TextKey = "Props.Snack1",
        // 只需要填与样例不同的字段
    });

    reg.Add(new PropData
    {
        PropID = "Snack2",
        TextKey = "Props.Snack2"
    });
} // using 结束时自动调用 RegisterProps
```

#### `PropData` 类

- **命名空间**：`YuanAPI`
- **类型**：`public class PropData`
- **类说明**：描述一个物品的完整数据（价格、类别、效果、本地化键、预制体路径等）。

- **属性**：
  - `public string PropNamespace { get; set; } = PropRegistry.DefaultNamespace;`
    - 物品命名空间，建议使用你模组独有的前缀。
  - `public string PropID { get; set; }`
    - 物品在命名空间内的唯一 ID。
  - `public string Uid => $"{PropNamespace}:{PropID}";`
    - 只读属性，返回组合后的 UID。
  - `public int? Price { get; set; } = null;`
    - 物品价格。
  - `public int? Category { get; set; } = null;`
    - 物品分类，对应 `PropCategory` 的整数值。
  - `public Dictionary<int, int> PropEffect { get; set; } = new();`
    - 属性效果映射，键为 `PropEffectType` 的整数值，值为加成数值。
  - `public string TextNamespace { get; set; } = Localization.DefaultNamespace;`
    - 本地化命名空间。
  - `public string TextKey { get; set; }`
    - 本地化键。
  - `public string PrefabPath { get; set; }`
    - 预制体在 `Resources` 或资产包中的路径，例如 `"AllProp/1001"` 或 `"MyModAssets/Props/Item1"`。

- **方法**：
  - `public bool IsValid()`
    - **功能**：检查数据是否合法（命名空间/ID/本地化信息/PrefabPath/价格/分类等均非空）。
  - `internal static PropData FromVanillaPropData(List<string> listData, int index)`
    - **功能**：从原版的物品数据列表构建 `PropData`；一般只供内部使用。
  - `internal List<string> ToVanillaPropData()`
    - **功能**：将 `PropData` 转回原版使用的数据列表，不合法时抛出 `InvalidDataException`。
  - `public static PropData operator +(PropData sample, PropData that)`
    - **功能**：将 `that` 覆盖到 `sample` 上生成新物品，常用于基于样例物品批量生成变种。

#### `PropCategory` 枚举

- **命名空间**：`YuanAPI`
- **类型**：`public enum PropCategory`
- **类说明**：物品大类枚举，与你的物品在游戏中的分类对应。

（枚举值较多，仅举例）

- `Offering`（祭品，0）
- `Fertilizer`（肥料，1）
- `Food`（粮肉，2）
- `Snack`（美食，3）
- `Weapon`（武器，13）
- `Medicine`（丹药，23）

#### `PropEffectType` 枚举

- **命名空间**：`YuanAPI`
- **类型**：`public enum PropEffectType`
- **类说明**：物品属性加成类型枚举，对应 `PropEffect` 字典中的键。

示例值：

- `Writing`（文，0）
- `Might`（武，1）
- `Health`（健康，4）
- `Mood`（心情，5）
- `Charisma`（魅力，6）
- `Life`（寿命，8）
- `CraftSkill`（工，15）

#### Harmony 补丁类（高级用法）

这些类主要用于 YuanAPI 内部挂载 Harmony 补丁，一般不需要直接调用。

- `YuanAPI.PropRegistryPatches.ResourcesPatch`
  - **功能**：在 `Resources.Load("AllProp/{id}")` 时重写路径到 `PropData.PrefabPath`。
- `YuanAPI.PropRegistryPatches.SaveDataPatch`
  - **功能**：
    - 存档时将大于原版数量的物品 ID 转成 UID 存入文件。
    - 读档时将 UID 解析为内部索引，无法解析的条目会被记录错误并丢弃。

### 常见问题（FAQ）

- **Q：如何只修改原版物品而不新增？**  
  **A**：构造 `PropData` 时将 `PropNamespace` 设为 `PropRegistry.VanillaNamespace`，`PropID` 设为原版物品的数字 ID（字符串），其余属性按需填写。该数据会被放入 `_patchedVanillaProps`，并在注入阶段覆盖原版物品。

- **Q：如何给新物品添加本地化？**  
  **A**：推荐做法是：
  1. 在 `Localization` 的 JSON 中为你的物品添加键，例如 `MyMod.Props.MyFirstProp.Name`。
  2. 在 `PropData` 中将 `TextNamespace = "MyMod"`，`TextKey = "Props.MyFirstProp.Name"`。

- **Q：移除一个模组后，旧存档还能读取吗？**  
  **A**：存档中的 UID 会在读档时通过 `PropRegistry.TryGetIndex` 解析；如果对应模组未加载，将无法解析并在日志中记录错误，同时跳过该物品。原版物品不会受影响。

- **Q：批量注册很多物品时有什么建议？**  
  **A**：可以使用 `PropRegistry.CreateInstance` 和 `PropRegistryInstance`，先定义一个样例物品，然后只为每个物品填差异字段，最后在 `Dispose` 时一次性注册，代码更简洁。

