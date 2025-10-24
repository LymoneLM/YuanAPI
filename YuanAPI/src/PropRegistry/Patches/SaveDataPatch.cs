using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace YuanAPI.PropRegistryPatches;

[HarmonyPatch]
public class SaveDataPatch
{
    /// <summary>
    /// 存档时将库存物品数字ID切换为UID存入存档
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveData), nameof(SaveData.SaveGameData))]
    public static void SaveGameDataPostfix()
    {
        YuanLogger.LogDebug("SaveGameData to UID");

        var propHave = Mainload.Prop_have.Select(prop => new List<string>(prop)).ToList();
        var propCount = PropRegistry.VanillaPropCount;

        propHave.ForEach(prop =>
        {
            if (!int.TryParse(prop[0], out var propID))
            {
                YuanLogger.LogError($"SaveData: 无法解析数据ID{prop[0]}，这可能不是YuanAPI导致的，本次保存将跳过该数据");
                propHave.Remove(prop);
                return;
            }

            if (propID >= propCount)
            {
                prop[0] = PropRegistry.GetUid(propID);
            }
        });

        ES3.Save<List<List<string>>>("Prop_have", propHave, Mainload.CunDangIndex_now + "/GameData.es3");
    }

    /// <summary>
    /// 读取存档时将物品从UID解析成数字ID
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveData), nameof(SaveData.ReadGameData))]
    public static void ReadGameDataPostfix()
    {
        YuanLogger.LogDebug("ReadGameData in UID");

        var propHave = Mainload.Prop_have;
        var propCount = PropRegistry.VanillaPropCount;

        propHave.ForEach(prop =>
        {
            if (int.TryParse(prop[0], out var id) && id >= 0 && id < propCount)
                return;
            if (!PropRegistry.TryGetIndex(prop[0], out var index))
            {
                YuanLogger.LogError($"SaveData: 无法解析数据ID{prop[0]}，请检查是否有模组未加载，本次加载将跳过该数据");
                propHave.Remove(prop);
            }
            else
            {
                prop[0] = index.ToString();
            }
        });
    }
}
