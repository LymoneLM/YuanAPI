using System;
using System.Collections.Generic;
using YuanAPI.PropRegistryPatches;

namespace YuanAPI;

internal class PropException(string message) : Exception(message) { }

[Submodule]
public class PropRegistry : IDisposable
{
    // 静态变量
    internal static List<PropData> AllProps { get; set; } = [];
    internal static int VanillaPropCount { get; set; }
    internal static HashSet<string> AllModID { get; set; } = [];

    internal static Dictionary<string, int> Uid2Index { get; set; } = new Dictionary<string, int>();

    // 实例变量
    public string ModID { get; set; }
    public List<PropData> PropList { get; set; }

    #region 静态方法

    public static void SetHooks()
    {
        YuanLogger.LogDebug("PropRegistry SetHooks Running");

        ResourceRegistry.SetHooks();

        YuanAPIPlugin.Harmony.PatchAll(typeof(ResourcesPatch));
        YuanAPIPlugin.Harmony.PatchAll(typeof(SaveDataPatch));

        YuanAPIPlugin.OnStart += InjectMainload;
    }
    private static void InjectMainload()
    {
        VanillaPropCount = Mainload.AllPropdata.Count;

        if (AllProps.Count == 0)
            return;

        foreach (var prop in AllProps)
        {
            Mainload.AllPropdata.Add(prop.ToVanillaPropDataList());
            AllText.Text_AllProp.Add(prop.Text); //TODO: 改用L10N

            YuanLogger.LogDebug($"添加物品{prop.Text[0]}");
        }
    }

    #endregion

    #region 实例方法

    public PropRegistry(string modID, List<PropData> propList = null)
    {
        this.ModID = modID;
        this.PropList = propList ?? [];
    }

    public void Add(PropData prop)
    {
        PropList.Add(prop);
    }

    public void Dispose()
    {
        YuanLogger.LogDebug("PropRegistry Dispose");
        RegisterProps();
    }

    public void RegisterProps()
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

    #endregion

}
