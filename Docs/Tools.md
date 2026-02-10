### 模块概览

- **功能简介**：`Tools` 模块提供一组便捷的游戏工具封装，包括：
  - `PropTool`：向玩家库存添加物品。
  - `CoinTool`：安全修改游戏货币。
  - `MsgTool`：在游戏界面显示提示消息。
- **模块依赖**：
  - 游戏本体类型：`Mainload`, `FormulaData`, `AllText`, `SetPanel` 等。
  - [物品注册器](PropRegistry.md)（间接依赖，用于正确的物品索引）。
- **版本状态**：接口简单稳定，可安全在模组中使用；后续可能增加更多工具方法。

### 快速开始

- **引入方式**：
  - 引用 YuanAPI DLL，并在代码中添加：
    - `using YuanAPI.Tools;`
  - 在任意 BepInEx 插件代码中直接调用这些静态/实例方法。

- **最小示例**：给玩家加钱并添加一个物品

```csharp
using BepInEx;
using YuanAPI;
using YuanAPI.Tools;

[BepInPlugin("com.example.tooldemo", "Tools Demo", "1.0.0")]
public class ToolsDemoPlugin : BaseUnityPlugin
{
    private readonly CoinTool _coinTool = new CoinTool();

    private void Awake()
    {
        // 1. 给玩家加 1000 金钱（若为负数则按 TryChangeCoins 逻辑检查余额）
        if (_coinTool.TryChangeCoins(1000))
        {
            MsgTool.TipMsg("获得 1000 金钱！", TipLv.Info);
        }

        // 2. 使用 PropRegistry 获取物品索引，然后用 PropTool 添加物品
        // 假设你的模组已有一个 UID 为 "MyMod:MyFirstProp" 的物品
        if (PropRegistry.TryGetIndex("MyMod:MyFirstProp", out var index))
        {
            var success = PropTool.AddProp(index, 1, storage: true, silence: false);
            if (!success)
            {
                MsgTool.TipMsg("背包空间不足，添加物品失败。", TipLv.Warning);
            }
        }
    }
}
```

### 使用注意事项

- **线程安全性**：
  - 所有工具都直接操作游戏内部状态（如 `Mainload.Prop_have`, `Mainload.FamilyData`, `Mainload.Tip_Show`），必须在 Unity 主线程中调用。
  - 不要在后台线程中调用这些 API，以免引起数据竞争或崩溃。
- **生命周期**：
  - 一般在以下阶段调用较为安全：
    - 游戏存档加载完成之后（确保 `Mainload` 等单例已经初始化）。
    - 某些游戏事件回调中（如按钮点击、剧情触发）。
  - 避免在游戏尚未初始化完、或正在读写存档的关键时刻频繁调用。
- **兼容性提示**：
  - 对 `Mainload`、`AllText` 等结构有依赖，若游戏更新导致这些结构变更，YuanAPI 可能需要同步更新。
  - 与修改相同数据结构的其他模组同时使用时，应留意最终数据的一致性（例如多模组同时向 `Prop_have` 写入）。
- **与其他模组的交互**：
  - `PropTool.AddProp` 与 `PropRegistry` 相配合使用，可以安全处理由多个模组注册的物品。
  - `MsgTool.TipMsg` 只是向 `Mainload.Tip_Show` 队列追加消息，通常不会与其他模组冲突。

### API 参考

#### `TipLv` 枚举

- **命名空间**：`YuanAPI.Tools`
- **类型**：`public enum TipLv`
- **类说明**：提示消息的类型等级，用于在游戏中区分信息/警告等。

- **枚举值**：
  - `Info`：普通信息。
  - `Warning`：警告信息。
  - `ShortInfo`：短暂显示的信息。

#### `MsgTool` 类

- **命名空间**：`YuanAPI.Tools`
- **类型**：`public static class MsgTool`
- **类说明**：在游戏画面上方显示提示信息的工具类。

- **方法**：
  - `public static void TipMsg(string msg, TipLv lv = TipLv.Info)`
    - **功能**：向游戏 UI 的提示队列添加一条消息。
    - **参数**：
      - `msg`：消息内容，非空字符串，否则不会添加。
      - `lv`：消息类型等级，默认 `TipLv.Info`。
    - **行为**：
      - 将 `[((int)lv).ToString(), msg]` 添加到 `Mainload.Tip_Show` 列表。

- **示例**：

```csharp
MsgTool.TipMsg("操作成功！", TipLv.Info);
MsgTool.TipMsg("背包空间不足！", TipLv.Warning);
```

#### `PropTool` 类

- **命名空间**：`YuanAPI.Tools`
- **类型**：`public static class PropTool`
- **类说明**：提供向背包添加物品的工具方法，封装容量检查与数量合并逻辑。

- **方法**：
  - `public static bool AddProp(int propId, int propCount, bool storage = true, bool silence = false)`
    - **功能**：向玩家库存中添加指定 ID 的物品。
    - **参数**：
      - `propId`：物品内部索引 ID（对应 `Mainload.AllPropdata` 与 `PropRegistry.GetIndex` 的索引）。
      - `propCount`：添加数量。
      - `storage`：是否占用“仓储空间”，若为 `true` 会检查/消耗 `Mainload.FamilyData[5]`。
      - `silence`：当容量不足时是否静默（`true` 不显示提示，`false` 使用 `MsgTool` 提示）。
    - **返回值**：
      - `true`：添加成功。
      - `false`：ID 非法或容量不足。
  - `public static bool AddProp(string propId, int propCount, bool storage = true, bool silence = false)`
    - **功能**：以字符串形式提供物品 ID 的重载；内部简单调用 `int.Parse` 转换为整型再执行同样逻辑。

- **异常**：
  - 字符串重载中的 `propId` 不能解析为整数时会抛出 `FormatException` 或 `OverflowException`。

- **示例**：

```csharp
// 基于 PropRegistry 的 UID 获取索引后添加物品
if (PropRegistry.TryGetIndex("MyMod:SpecialItem", out var idx))
{
    PropTool.AddProp(idx, 5);
}
```

#### `CoinTool` 类

- **命名空间**：`YuanAPI.Tools`
- **类型**：`public class CoinTool`
- **类说明**：封装游戏货币（银两等）的修改逻辑，提供“尝试修改”的安全接口。

- **方法**：
  - `public bool TryChangeCoins(int count)`
    - **功能**：尝试变更玩家货币数量。
    - **参数**：
      - `count`：变更值，正数为增加，负数为扣除。
    - **返回值**：
      - `true`：当 `count >= 0` 或当前货币数足以扣除（`FormulaData.GetCoinsNum() >= -count`）时，调用 `FormulaData.ChangeCoins(count)` 并返回 `true`。
      - `false`：余额不足，未做任何修改。

- **示例**：

```csharp
var coinTool = new CoinTool();
if (!coinTool.TryChangeCoins(-500))
{
    MsgTool.TipMsg("银两不足，操作失败。", TipLv.Warning);
}
```

### 常见问题（FAQ）

- **Q：`PropTool.AddProp` 的 `propId` 是 UID 吗？**  
  **A**：不是，是内部整数索引（与 `Mainload.AllPropdata` 一致）。如果你只有 UID，可先通过 `PropRegistry.GetIndex(uid)` 或 `TryGetIndex` 获取索引，再传给 `AddProp`。

- **Q：为什么添加物品会失败但没有提示？**  
  **A**：如果 `storage == true` 且仓储空间不足，同时你将 `silence == true`，方法会直接返回 `false` 而不弹出提示。若想看到提示，请将 `silence` 设为 `false`。

- **Q：能否在线程池任务中调用这些工具？**  
  **A**：不建议。这些工具直接读写 Unity 与游戏数据结构，应该只在主线程中调用。

