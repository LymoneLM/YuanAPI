using System;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace YuanAPI.PropRegistryPatches;

//Loading custom prop
[HarmonyPatch]
public static class ResourcesPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Normal)]
    [HarmonyPatch(typeof(Resources), nameof(Resources.Load), typeof(string), typeof(Type))]
    public static bool Prefix(ref string path, Type systemTypeInstance, ref Object __result, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (!path.StartsWith("AllProp/"))
            return true;

        var propID = int.Parse(path.Substring("AllProp/".Length));

        if (propID < PropRegistry.VanillaPropCount)
            return true;
        propID = propID - PropRegistry.VanillaPropCount;
        path = PropRegistry.AllProps[propID].PrefabPath;

        return true;
    }
}
