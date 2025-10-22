using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace YuanAPI.PropRegistryPatches;

[HarmonyPatch]
public class MainloadPatch
{
    /// <summary>
    /// 在Mainload初始化后注入数据
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Mainload), "LoadData")]
    public static void Postfix()
    {
        PropRegistry.VanillaPropCount = Mainload.AllPropdata.Count;
        // PropRegistry.ModOffsets.ForEach(offset => offset += vanilla);
        // PropRegistry.PropId2Index = new Dictionary<int, int>();

        if (PropRegistry.AllProps.Count == 0)
            return;

        for (var i = 0; i < PropRegistry.AllProps.Count; i++)
        {
            Mainload.AllPropdata.Add(PropRegistry.AllProps[i].ToVanillaPropDataList());
            AllText.Text_AllProp.Add(PropRegistry.AllProps[i].Text); //TODO: 安全检查

            YuanLogger.LogDebug($"添加物品{PropRegistry.AllProps[i].Text[0]}");
        }
    }
}
