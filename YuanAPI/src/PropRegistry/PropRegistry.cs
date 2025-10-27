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
    private static Dictionary<(string ns, string id), int> _uidMap = new();

    internal static int VanillaPropCount { get; private set; }

    public const string DefaultNamespace = "Common";
    public const string VanillaNamespace = "Vanilla";


    #region static methods

    public static void Initialize()
    {
        YuanLogger.LogDebug("PropRegistry Initialize Called");

        ResourceRegistry.Initialize();
        Localization.Initialize();

        YuanAPIPlugin.Harmony.PatchAll(typeof(ResourcesPatch));
        YuanAPIPlugin.Harmony.PatchAll(typeof(SaveDataPatch));

        YuanAPIPlugin.OnStart += InjectMainload;
    }

    [NoInit]
    public static PropData GetProp(string @namespace, string id)
    {
        return _allProps[_uidMap[(@namespace, id)]];
    }

    [NoInit]
    public static PropData GetProp(string uid)
    {
        var list = uid.Split(':');
        return _allProps[_uidMap[(list[0], list[1])]];
    }

    [NoInit]
    public static PropData GetProp(int index)
    {
        return _allProps[index];
    }

    [NoInit]
    public static string GetUid(int index)
    {
        return _allProps[index].PropID;
    }

    [NoInit]
    public static bool TryGetUid(int index, out (string ns, string id) uid)
    {
        var success = index >= 0 && index < _allProps.Count;
        uid = (_allProps[index].PropNamespace, _allProps[index].PropID);
        return success;
    }

    [NoInit]
    public static int GetIndex(string @namespace, string id)
    {
        return _uidMap[(@namespace, id)];
    }

    [NoInit]
    public static bool TryGetIndex(string @namespace, string id, out int index)
    {
        return _uidMap.TryGetValue((@namespace, id), out index);
    }

    [NoInit]
    public static bool TryGetIndex(string uid, out int index)
    {
        var list = uid.Split(':');
        if (list.Length == 2)
            return _uidMap.TryGetValue((list[0], list[1]), out index);

        index = -1;
        return false;
    }

    private static void InjectMainload()
    {
        // 添加新增物品本地化字串
        foreach (var prop in _allProps)
            AllText.Text_AllProp.Add(Localization.GetTextAllLocales(prop.TextNamespace, prop.TextKey));

        // 载入原版物品
        VanillaPropCount = Mainload.AllPropdata.Count;
        var vanilla= Mainload.AllPropdata.Select(PropData.FromVanillaPropData).ToList();
        vanilla.AddRange(_allProps);
        _allProps = vanilla;

        var uidList =  _uidMap.Keys.ToList();
        uidList.Do(str => _uidMap[str] += VanillaPropCount);
        for (var i = 0; i < VanillaPropCount; i++)
        {
            _uidMap.Add((VanillaNamespace, i.ToString()), i);
        }

        // 处理对原版物品的修改
        foreach (var prop in _patchedVanillaProps)
        {
            if (!int.TryParse(prop.PropID, out var index) || index < 0 || index >= VanillaPropCount)
                continue;
            _allProps[index] = prop;
            AllText.Text_AllProp[index] = Localization.GetTextAllLocales(prop.TextNamespace, prop.TextKey);
        }

        Mainload.AllPropdata = _allProps.Select(prop => prop.ToVanillaPropData()).ToList();

        YuanLogger.LogDebug($"PropRegistry: 添加了{Mainload.AllPropdata.Count-VanillaPropCount}个物品");
    }

    /// <summary>
    /// 注册物品数据
    /// </summary>
    /// <param name="props">物品数据列表</param>
    public static void RegisterProps(List<PropData> props)
    {
        var count = _allProps.Count;
        foreach (var prop in props)
        {
            if (!prop.IsValid())
            {
                YuanLogger.LogError($"PropRegistry: 名为 {prop.PropNamespace}:{prop.PropID??""} 的数据为空或非法，将跳过注册");
                continue;
            }

            if (prop.PropNamespace == VanillaNamespace)
            {
                _patchedVanillaProps.Add(prop);
                continue;
            }

            // 后读覆盖
            if (_uidMap.TryGetValue((prop.PropNamespace,prop.PropID), out var index))
            {
                _allProps[index] = prop;
            }
            else
            {
                _allProps.Add(prop);
                _uidMap.Add((prop.PropNamespace,prop.PropID), count++);
            }
        }
    }

    #endregion

    #region instance methods

    /// <summary>
    /// 创建一个实例，提供方便小批量内联导入物品数据的方法 <br/>
    /// 可选填充一个样例，每次加入都会尝试覆盖合并样例 <br/><br/>
    /// 建议使用using或者显式调用Dispose用于结束添加并注册 <br/>
    /// </summary>
    /// <param name="sample">样例，可选</param>
    /// <param name="propList">物品数据集，可选</param>
    /// <returns></returns>
    public static PropRegistryInstance CreateInstance(PropData sample = null, List<PropData> propList = null)
        => new(sample, propList);

    public class PropRegistryInstance : IDisposable
    {
        public PropData Sample {get; set;}
        private List<PropData> _propList = [];
        private bool _disposed;

        internal PropRegistryInstance(PropData sample, List<PropData> propList)
        {
            Sample = sample;
            propList?.ForEach(Add);
        }

        public void Add(PropData prop)
        {
            _propList.Add(Sample + prop);
        }

        public void Dispose()
        {
            if (_disposed)  return;
            YuanLogger.LogDebug("PropRegistryInstance Dispose");
            RegisterProps(_propList);
            _disposed = true;
        }

    }

    #endregion

}
