using System.Collections.Generic;
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

        var propHave = Mainload.Prop_have.ConvertAll(prop => new List<string>(prop));
        var propCount = PropRegistry.VanillaPropCount;

        propHave.ForEach(prop =>
        {
            if (!int.TryParse(prop[0], out var propID))
            {
                YuanLogger.LogError($"SaveData: 无法解析数据ID{prop[0]}，将跳过保存，数据将丢失");
                propHave.Remove(prop);
                return;
            }

            if (propID >= propCount)
            {
                prop[0] = PropRegistry.AllProps[propID-propCount].ToString();
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
            if (int.TryParse(prop[0], out var _))
                return;
            if (!PropRegistry.Uid2Index.TryGetValue(prop[0], out var propID))
            {
                YuanLogger.LogError($"SaveData: 无法解析数据ID{prop[0]}，将跳过载入，数据将丢失");
                return;
            }
            prop[0] =  (propID+propCount).ToString();
        });
    }
}
