using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using YuanAPI.PropRegistryPatches;

namespace YuanAPI;

[Submodule]
public class PropRegistry
{
    // 静态变量
    private static List<PropData> _allProps = [];
    private static List<PropData> _patchedVanillaProps = [];
    private static Dictionary<string, int> _uid2Index = new();

    internal static int VanillaPropCount { get; private set; }

    private const string DefaultNamespace = "Common";
    private const string VanillaClassName = "Vanilla";


    #region static methods

    public static void Initialize()
    {
        YuanLogger.LogDebug("PropRegistry SetHooks Running");

        ResourceRegistry.Initialize();

        YuanAPIPlugin.Harmony.PatchAll(typeof(ResourcesPatch));
        YuanAPIPlugin.Harmony.PatchAll(typeof(SaveDataPatch));

        YuanAPIPlugin.OnStart += InjectMainload;
    }

    [NoInit]
    public static PropData GetProp(string uid)
    {
        return _allProps[_uid2Index[uid]];
    }

    [NoInit]
    public static PropData GetProp(int index)
    {
        return _allProps[index];
    }

    [NoInit]
    public static bool TryGetProp(string uid , out PropData prop)
    {
        var success = _uid2Index.TryGetValue(uid, out var index);
        prop = success ? _allProps[index] : null;
        return success;
    }

    [NoInit]
    public static bool TryGetProp(int index, out PropData prop)
    {
        var success = index >= 0 && index < _allProps.Count;
        prop = success ? _allProps[index] : null;
        return success;
    }

    [NoInit]
    public static string GetUid(int index)
    {
        return _allProps[index].ID;
    }

    [NoInit]
    public static bool TryGetUid(int index, out string uid)
    {
        var success = index >= 0 && index < _allProps.Count;
        uid = _allProps[index].ID;
        return success;
    }

    [NoInit]
    public static int GetIndex(string uid)
    {
        return _uid2Index[uid];
    }

    [NoInit]
    public static bool TryGetIndex(string uid, out int index)
    {
        return _uid2Index.TryGetValue(uid, out index);
    }

    private static void InjectMainload()
    {
        VanillaPropCount = Mainload.AllPropdata.Count;

        var vanilla= Mainload.AllPropdata.Select(PropData.FromVanillaPropData).ToList();
        vanilla.AddRange(_allProps);
        _allProps = vanilla;
        //TODO: 补充L10N

        _uid2Index.Keys.Do(key => _uid2Index[key] += VanillaPropCount);
        for (var i = 0; i < VanillaPropCount; i++)
        {
            _uid2Index.Add($"{VanillaClassName}:{i}", i);
        }

        foreach (var prop in _patchedVanillaProps)
        {
            if (int.TryParse(prop.ID, out var index) && index >= 0 && index < VanillaPropCount)
            {
                prop.ID = $"{DefaultNamespace}:{index}";
                _allProps[index] = prop;
                //TODO: 补充L10N
            }
        }

        Mainload.AllPropdata = _allProps.Select(prop => prop.ToVanillaPropData()).ToList();

        YuanLogger.LogDebug($"PropRegistry: 添加了{Mainload.AllPropdata.Count-VanillaPropCount}个物品");
    }

    #endregion

    #region instance methods

    public static PropRegistryInstance CreateInstance(string @namespace = DefaultNamespace, List<PropData> propList = null)
    {
        return new PropRegistryInstance(@namespace, propList ?? []);
    }

    public class PropRegistryInstance : IDisposable
    {
        public string Namespace { get; private set; }
        public List<PropData> PropList { get; private set; }

        internal PropRegistryInstance(string @namespace, List<PropData> propList)
        {
            Namespace = @namespace;
            PropList = propList;
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
            if (string.IsNullOrWhiteSpace(Namespace))
                Namespace = DefaultNamespace;

            if (Namespace == VanillaClassName)
            {
                foreach (var prop in PropList)
                {
                    if (!prop.IsValid())
                    {
                        YuanLogger.LogError($"PropRegistry: 名为 {Namespace}:{prop.ID} 的数据为空或非法，将跳过注册");
                        continue;
                    }
                    _patchedVanillaProps.Add(prop);
                }
            }

            var count = _allProps.Count;
            foreach (var prop in PropList)
            {
                if (!prop.IsValid())
                {
                    YuanLogger.LogError($"PropRegistry: 位于 {Namespace}:{prop.ID} 的数据为空或非法，将跳过注册");
                    continue;
                }

                var uid = $"{Namespace}:{prop.ID}";
                prop.ID = uid;
                if (_uid2Index.TryGetValue(uid, out var index))
                {
                    _allProps[index] = prop;
                }
                else
                {
                    _allProps.Add(prop);
                    _uid2Index.Add(uid, count++);
                }
            }
        }
    }

    #endregion

}
