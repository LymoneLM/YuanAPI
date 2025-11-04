// Source code is taken from CommonAPI (GPL-3.0) - https://github.com/limoka/CommonAPI

using System;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace YuanAPI.ResourceRegistryPatches;

//Loading custom resources
[HarmonyPatch]
public static class ResourcesPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(Resources), nameof(Resources.Load), typeof(string), typeof(Type))]
    public static bool Prefix(ref string path, Type systemTypeInstance, ref Object __result, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        foreach (var resource in ResourceRegistry.ModResources)
        {

            if (!path.Contains(resource.KeyWord) || !resource.HasAssetBundle())
                continue;

            if (resource.Bundle.Contains(path + ".prefab"))
            {
                var myPrefab = resource.Bundle.LoadAsset(path + ".prefab");
                YuanLogger.LogDebug($"Loading registered asset {path}: {(myPrefab != null ? "Success" : "Failure")}");

                __result = myPrefab;
                return false;
            }

            foreach (var extension in ResourceRegistry.SpriteFileExtensions)
            {
                if (!resource.Bundle.Contains(path + extension))
                    continue;

                var mySprite = resource.Bundle.LoadAsset(path + extension, systemTypeInstance);

                YuanLogger.LogDebug($"Loading registered asset {path}: {(mySprite != null ? "Success" : "Failure")}");

                __result = mySprite;
                return false;
            }

            foreach (var extension in ResourceRegistry.AudioClipFileExtensions)
            {
                if (!resource.Bundle.Contains(path + extension))
                    continue;

                var myAudioClip = resource.Bundle.LoadAsset(path + extension, systemTypeInstance);

                YuanLogger.LogDebug($"Loading registered asset {path}: {(myAudioClip != null ? "Success" : "Failure")}");

                __result = myAudioClip;
                return false;
            }
        }

        return true;
    }
}
