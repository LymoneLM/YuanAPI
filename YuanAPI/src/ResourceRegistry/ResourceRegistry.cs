using System;
using System.Collections.Generic;
using YuanAPI.ResourceRegistryPatches;

namespace YuanAPI;

/// <summary>
/// Indicates that loading something has failed
/// </summary>
public class ResourceException(string message) : Exception(message) { }

[Submodule]
public class ResourceRegistry
{
    internal static List<ResourceData> ModResources = [];

    internal static string[] SpriteFileExtensions = [".jpg", ".png", ".tif"];
    internal static string[] AudioClipFileExtensions = [".mp3", ".ogg", ".waw", ".aif"];

    public static void Initialize()
    {
        YuanAPIPlugin.Harmony.PatchAll(typeof(ResourcesPatch));
    }

    /// <summary>
    /// Registers mod resources for loading
    /// </summary>
    /// <param name="resource"></param>
    public static void AddResource(ResourceData resource) {
        ModResources.Add(resource);
    }
}
