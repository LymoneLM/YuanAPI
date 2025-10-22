using System;
using System.Collections.Generic;
using YuanAPI.PropRegistryPatches;

namespace YuanAPI;

internal class PropException(string message) : Exception(message) { }

[Submodule]
public class PropRegistry : IDisposable
{
    // 静态部分
    internal static List<PropData> AllProps { get; set; } = [];
    internal static int VanillaPropCount { get; set; }
    internal static HashSet<string> AllModID { get; set; } = [];

    internal static Dictionary<string, int> Uid2Index { get; set; } = new Dictionary<string, int>();
    // internal static Dictionary<int, int> PropId2Index { get; set; }

    // internal static List<int> ModPropCount { get; set; } = [];
    // internal static List<int> ModOffsets { get; set; } = [0];

    // 实例变量
    public string ModID { get; set; }
    public List<PropData> PropList { get; set; }

    public static void SetHooks()
    {
        YuanLogger.LogDebug("PropRegistry SetHooks Running");

        ResourceRegistry.SetHooks();

        YuanAPIPlugin.Harmony.PatchAll(typeof(ResourcesPatch));
        YuanAPIPlugin.Harmony.PatchAll(typeof(MainloadPatch));
        YuanAPIPlugin.Harmony.PatchAll(typeof(SaveDataPatch));
    }

    public PropRegistry(string modID, List<PropData> propList = null)
    {
        this.ModID = modID;
        this.PropList = propList ?? [];
    }

    #region IDisposable 实现

    public void Dispose()
    {
        YuanLogger.LogDebug("PropRegistry Dispose");
        LoadPropsToAllProps();
    }

    #endregion

    public void LoadPropsToAllProps()
    {
        if (string.IsNullOrEmpty(ModID))
            throw new PropException("加载失败：该模组没有ModID");
        if (!AllModID.Add(ModID))
            throw new PropException("加载失败：重复的ModID: "+ModID);

        var count = AllProps.Count;
        var idSet =  new HashSet<string>();
        for (var i = 0; i < PropList.Count; i++)
        {
            if (PropList[i] == null || !PropList[i].IsValid())
            {
                YuanLogger.LogError($"PropRegistry: 模组 {ModID} 的数据 {i} 无效或非法，将跳过加载");
                continue;
            }

            if (!idSet.Add(PropList[i].ID))
            {
                YuanLogger.LogError($"PropRegistry: 模组 {ModID} 具有重复的ID {PropList[i].ID} ，将跳过加载");
                continue;
            }

            AllProps.Add(PropList[i]);
            Uid2Index.Add($"{ModID}:{PropList[i].ID}", count++);
        }
    }
}
