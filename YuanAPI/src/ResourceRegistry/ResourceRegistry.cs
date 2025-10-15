using YuanAPI.Patches;
using System;
using System.Collections.Generic;

namespace YuanAPI;

/// <summary>
/// Indicates that loading something has failed
/// </summary>
public class LoadException(string message) : Exception(message) { }

[Submodule]
public class ResourceRegistry
{
    internal static List<ResourceData> ModResources = [];

    internal static string[] SpriteFileExtensions = [".jpg", ".png", ".tif"];
    internal static string[] AudioClipFileExtensions = [".mp3", ".ogg", ".waw", ".aif"];

    public static void SetHooks()
    {
        YuanAPI.Harmony.PatchAll(typeof(ResourcesPatch));
    }
}
