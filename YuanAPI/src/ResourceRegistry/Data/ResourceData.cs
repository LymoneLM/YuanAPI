// Source code is taken from CommonAPI (GPL-3.0) - https://github.com/limoka/CommonAPI

using System.IO;
using System.Reflection;
using UnityEngine;

namespace YuanAPI;
public class ResourceData
{
    public string ModId;
    public string KeyWord;
    public string ModPath;

    public AssetBundle Bundle;

    /// <summary>
    /// Create new resource definition
    /// </summary>
    /// <param name="modId">Your mod ID</param>
    /// <param name="keyWord">Unique Keyword used only by your mods</param>
    /// <param name="modPath">Path to mod's main assembly</param>
    public ResourceData(string modId, string keyWord, string modPath)
    {
        this.ModId = modId;
        this.ModPath = modPath;
        this.KeyWord = keyWord;
    }

    /// <summary>
    /// Create new resource definition. Path is inferred from what assembly is calling.
    /// </summary>
    /// <param name="modId">Your mod ID</param>
    /// <param name="keyWord">Unique Keyword used only by your mods</param>
    public ResourceData(string modId, string keyWord)
    {
        this.ModId = modId;
        this.ModPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
        this.KeyWord = keyWord;
    }

    /// <summary>
    /// Does this resource definition have an asset bundle loaded
    /// </summary>
    public bool HasAssetBundle()
    {
        return Bundle != null;
    }

    /// <summary>
    /// Load asset bundle from mod path.
    /// </summary>
    /// <param name="bundleName">Bundle name</param>
    /// <exception cref="ResourceException">Thrown if loading an asset bundle has failed</exception>
    public void LoadAssetBundle(string bundleName)
    {
        Bundle = AssetBundle.LoadFromFile($"{ModPath}/{bundleName}");
        if (Bundle == null)
        {
            throw new ResourceException($"Failed to load asset bundle at {ModPath}/{bundleName}");
        }
    }
}

