using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YuanAPI.LocalizationPatches;

namespace YuanAPI;

[Submodule]
public class Localization
{
    private static Dictionary<(string loc, string ns, string key), string> _store = new();

    private static List<string> _locales = [];
    private static Dictionary<string, string> _localeShowNames = new();
    private static Dictionary<string, List<string>> _fallbackChains = new();
    private static Dictionary<string, List<string>> _searchOrders = new();

    public static event Action<string> OnLanguageChanged;

    public const string DefaultLocale  = "zh-CN";
    public const string DefaultNamespace = "Common";
    public const string VanillaNamespace = "Vanilla";

    public static void Initialize()
    {
        YuanLogger.LogDebug("Localization Initialize Called");

        YuanAPIPlugin.Harmony.PatchAll(typeof(SetPanelPatch));
        YuanAPIPlugin.Harmony.PatchAll(typeof(SaveDataPatch));

        RegisterLocale("zh-CN","简体中文", []);
        RegisterLocale("en-US","English(US)", ["zh-CN"]);
        LoadFromAllText();
        YuanAPIPlugin.OnStart += InjectAllText;
    }

    #region Vanilla Processed

    private static List<FieldInfo> _vanillaFields = [];
    private static Dictionary<string, int> _itemCounts = new();

    private static void LoadFromAllText()
    {
        _vanillaFields = typeof(AllText).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(List<List<string>>)).ToList();

        if (_vanillaFields.Count == 0)
        {
            YuanLogger.LogError("Localization：未能成功找到原版字段");
            return;
        }

        var langCount = ((List<List<string>>)_vanillaFields[0].GetValue(null))[0].Count;
        if (langCount != _locales.Count)
            YuanLogger.LogError($"Localization：原版的本地化语言数 {langCount} 与YuanAPI定义不一致，将尝试处理已定义语言，建议升级YuanAPI");

        foreach (var field in _vanillaFields)
        {
            var fieldName = field.Name;
            var list = (List<List<string>>)field.GetValue(null);
            _itemCounts[fieldName] = list.Count;
            list.ForEach((item, index) =>
            {
                if (item.Count == 0)
                {
                    _locales.ForEach(locale =>
                        _store[(locale, VanillaNamespace, $"{fieldName}.{index}")] = "");
                    return;
                }

                _locales.ForEach((locale, langIndex) =>
                {
                    _store[(locale, VanillaNamespace, $"{fieldName}.{index}")] = item[langIndex];
                });
            });
        }

        // 特殊字段处理
        // AllText.Text_AllShenFen
        var allSenFen = AllText.Text_AllShenFen;
        allSenFen.ForEach((group, gIndex) =>
        {
            group.ForEach((item, iIndex) =>
            {
                _locales.ForEach((locale, langIndex) =>
                {
                    _store[(locale, VanillaNamespace, $"Text_AllShenFen.{gIndex}.{iIndex}")] = item[langIndex];
                });
            });
        });

        YuanLogger.LogDebug($"Localization：成功读入{_vanillaFields.Count + 1}个字段");
    }

    private static void InjectAllText()
    {
        if (_vanillaFields == null || _vanillaFields.Count == 0)
        {
            YuanLogger.LogError("Localization：原版字段未能成功读入，拒绝注入");
            return;
        }

        foreach (var field in _vanillaFields)
        {
            var fieldName = field.Name;
            var count = _itemCounts[fieldName];
            List<List<string>> fieldList = [];
            for (var i = 0; i < count; i++)
            {
                fieldList.Add(_locales.Select(locale =>
                        GetText(locale, VanillaNamespace, $"{fieldName}.{i}")).ToList());
            }
            field.SetValue(null, fieldList);
        }

        // 特殊字段处理
        // AllText.Text_AllShenFen
        var gCount = AllText.Text_AllShenFen.Count;
        for (var i = 0; i < gCount; i++)
        {
            var iCount = AllText.Text_AllShenFen[i].Count;
            List<List<string>> itemList = [];
            for (var j = 0; j < iCount; j++)
            {
                itemList.Add(_locales.Select(locale =>
                        GetText(locale, VanillaNamespace, $"Text_AllShenFen.{i}.{j}")).ToList());
            }
            AllText.Text_AllShenFen[i] = itemList;
        }

        YuanLogger.LogDebug($"Localization：成功注入{_locales.Count}种语言");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 注册语言
    /// <param name="locale">语言代码</param>
    /// <param name="showName">语言的显示名</param>
    /// <param name="fallbackChain">指定该语言的回退链</param>
    /// </summary>
    public static void RegisterLocale(string locale, string showName, List<string> fallbackChain = null)
    {
        if (string.IsNullOrWhiteSpace(locale))
            throw new ArgumentException("locale 必须是非空字符串", nameof(locale));

        if (_locales.Contains(locale))
        {
            YuanLogger.LogWarning($"Localization： {locale} 语言重复注册，忽略本次注册");
        }
        else
        {
            _locales.Add(locale);
            _localeShowNames[locale] = showName;

            fallbackChain ??= [DefaultLocale];
            SetFallbackChain(locale, fallbackChain);

            _searchOrders.Clear();
        }
    }

    /// <summary>
    /// 设置某语言的回退链
    /// </summary>
    public static void SetFallbackChain(string locale, List<string> fallbackChain)
    {
        if (!_locales.Contains(locale))
            throw new ArgumentException("locale 未注册", nameof(locale));

        _fallbackChains[locale] = fallbackChain.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        EnsureNoCycles();
    }

    /// <summary>
    /// 加载指定路径下所有已注册语言的本地化数据，后读覆盖 <br/>
    /// 参考路径{path}/locales/{locale}/{namespace}.json <br/>
    /// <param name="path">locales文件夹所在路径</param>
    /// </summary>
    public static void LoadFromPath(string path)
    {
        var localesPath = Path.Combine(path, "locales");
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(localesPath))
            throw new DirectoryNotFoundException($"路径不存在：{localesPath}");

        foreach (var locale in _locales)
        {
            var localeDir = Path.Combine(localesPath, locale);
            if (!Directory.Exists(localeDir)) continue;

            foreach (var jsonFile in Directory.EnumerateFiles(localeDir, "*.json"))
            {
                var ns = Path.GetFileNameWithoutExtension(jsonFile);
                LoadOneFile(locale, ns, jsonFile);
            }
        }
    }

    /// <summary>
    /// 全量参数修改某条目，新增或覆盖
    /// </summary>
    /// <param name="loc">语言代码</param>
    /// <param name="ns">命名空间</param>
    /// <param name="key">条目的键值</param>
    /// <param name="value">条目的内容</param>
    public static void EditText(string loc, string ns, string key, string value)
    {
        _store[(loc, ns, key)] = value;
    }

    /// <summary>
    /// 根据索引获取语言代码
    /// </summary>
    /// <param name="index">索引</param>
    /// <returns>语言代码</returns>
    [NoInit]
    public static string GetLocale(int index)
    {
        return _locales[index];
    }

    /// <summary>
    /// 使用全部参数获取字串
    /// <param name="locale">语言代码</param>
    /// <param name="@namespace">命名空间</param>
    /// <param name="key">条目的键值</param>
    /// </summary>
    [NoInit]
    public static string GetText(string locale, string @namespace, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        var searchOrder = BuildSearchOrders(locale);
        foreach (var loc in searchOrder)
        {
            if (_store.TryGetValue((loc, @namespace, key), out var value))
                return value;
        }

        YuanLogger.LogWarning($"Localization: 无法解析 {locale}/{@namespace}:{key}");
        return $"{@namespace}:{key}";
    }

    [NoInit]
    public static List<string> GetTextAllLocales(string @namespace, string key)
        => _locales.Select(locale => GetText(locale, @namespace, key)).ToList();

    [NoInit]
    public static List<string> GetAllLocales()
    {
        return _locales;
    }

    [NoInit]
    public static List<string> GetAllShowNames()
        => _locales.Select(locale => _localeShowNames[locale]).ToList();

    [NoInit]
    public static int LocaleCount() => _locales.Count;

    /// <summary>
    /// 实例化，预设语言代码和命名空间以简化调用
    /// </summary>
    public static LocalizationInstance CreateInstance(string locale = DefaultLocale,
        string @namespace = DefaultNamespace, bool syncGlobalLocale = true)
        => new(locale, @namespace, syncGlobalLocale);


    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class LocalizationInstance
    {
        public string Locale { get; set; }
        public string Namespace { get; set; }

        public bool IsSyncGlobalLang
        {
            get;
            set
            {
                if (field==value)
                    return;
                if (value)
                    OnLanguageChanged += SyncLang;
                else
                    OnLanguageChanged -= SyncLang;
                field = value;
            }
        }

        internal LocalizationInstance(string locale, string @namespace, bool syncGlobalLocale)
        {
            Locale = locale;
            Namespace = @namespace;
            IsSyncGlobalLang = syncGlobalLocale;
        }

        private void SyncLang(string locale)
        {
            Locale = locale;
        }


        public string t(string key) => GetText(Locale, Namespace, key);
        public string t(string @namespace, string key) => GetText(Locale, @namespace, key);
        public string t(string locale, string @namespace, string key) => GetText(locale, @namespace, key);

        public string Get(string key) => GetText(Locale, Namespace, key);
        public string Get(string @namespace, string key) => GetText(Locale, @namespace, key);
        public string Get(string locale, string @namespace, string key) => GetText(locale, @namespace, key);
    }

    # endregion

    #region Private Methods

    internal static void CallLanguageChanged(string locale)
    {
        OnLanguageChanged?.Invoke(locale);
    }

    private static void LoadOneFile(string locale, string @namespace, string filePath)
    {
        try
        {
            using var fs = File.OpenRead(filePath);
            using var sr = new StreamReader(fs,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
                detectEncodingFromByteOrderMarks: true);
            using var reader = new JsonTextReader(sr);

            // 若 JSON 非法，会抛 JsonReaderException
            var root = JObject.Load(reader);
            if (root.Type != JTokenType.Object)
                throw new InvalidDataException($"根必须是对象：{filePath}");

            // 校验并拍平
            var flat = new Dictionary<string, string>();
            FlattenStringLeaves(root, flat, prefix: null);

            foreach (var kv in flat)
                _store[(locale, @namespace, kv.Key)] = kv.Value;
        }
        catch (JsonReaderException ex)
        {
            throw new InvalidDataException($"JSON 语法不合法（{ex.LineNumber}:{ex.LinePosition}) : {filePath}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"文件读取错误（{ex.GetType().Name}) : {filePath}", ex);
        }
    }

    private static void FlattenStringLeaves(JToken token, Dictionary<string, string> output, string prefix = null)
    {
        if (token is JObject obj)
        {
            foreach (var prop in obj.Properties())
            {
                var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                var val = prop.Value;

                switch (val.Type)
                {
                    case JTokenType.Object:
                        FlattenStringLeaves(val, output, key);
                        break;
                    case JTokenType.String:
                        output[key] = val.Value<string>() ?? string.Empty;
                        break;
                    default:
                        throw new InvalidDataException($"JSON 值必须为字符串（键：{key}，实际类型：{val.Type}）。");
                }
            }
        }
        else
        {
            throw new InvalidDataException("根必须是对象类型。");
        }
    }

    /// <summary>
    /// 构造当前语言的完整安全回退链
    /// </summary>
    /// <param name="locale">所需语言代码</param>
    /// <returns></returns>
    private static List<string> BuildSearchOrders(string locale)
    {
        if(_searchOrders.TryGetValue(locale, out var list))
            return list;

        var result = new List<string>();
        var visited = new HashSet<string>();

        Dfs(locale);
        result = result.Where(loc => _locales.Contains(loc)).ToList();

        _searchOrders[locale] = result;
        return result;

        void Dfs(string loc)
        {
            if (!visited.Add(loc)) return;
            result.Add(loc);
            if (!_fallbackChains.TryGetValue(loc, out var chain)) return;
            foreach (var n in chain.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                Dfs(n);
            }
        }
    }

    /// <summary>
    /// DFS检测全局回退链是否有环
    /// </summary>
    private static void EnsureNoCycles()
    {
        var visiting = new HashSet<string>();
        var visited = new HashSet<string>();

        foreach (var loc in _fallbackChains.Keys.Where(Dfs))
        {
            YuanLogger.LogError($"Localization: 检测到回退链路的环：起点 {loc}");
        }

        return;

        bool Dfs(string loc)
        {
            if (visiting.Contains(loc)) return true;   // found cycle
            if (visited.Contains(loc)) return false;

            visiting.Add(loc);
            if (_fallbackChains.TryGetValue(loc, out var list))
                if (list.Where(n => !string.IsNullOrWhiteSpace(n)).Any(Dfs))
                    return true;
            visiting.Remove(loc);
            visited.Add(loc);
            return false;
        }
    }

    #endregion

}
