using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YuanAPI.LocalizationPatches;

namespace YuanAPI;

[Submodule]
public class Localization
{
    private static Dictionary<(string loc, string ns, string key), string> _store = new();

    private static Dictionary<int, string> _index2Locale = new(); // 用于从原版获取语言信息
    private static Dictionary<string, string> _localeShowNames = new(); // 兼具语言存在性检查用
    private static Dictionary<string, List<string>> _fallbackChains = new();
    private static Dictionary<string, List<string>> _searchOrders = new();

    public static Action<string> LanguageChanged {get; set; }

    private const string DefaultLocale = "zh-CN";
    private const string DefaultNamespace = "Common";

    public static void Initialize()
    {
        YuanLogger.LogDebug("Localization Initialize Called");

        YuanAPIPlugin.Harmony.PatchAll(typeof(SetPanelPatch));

        RegisterLocale("zh-CN","简体中文", []);
        RegisterLocale("en-US","English(US)", ["zh-CN"]);
        LoadFromAllText();
        YuanAPIPlugin.OnStart += InjectAllText;
    }

    private static void LoadFromAllText()
    {
        // TODO: 加载原版本地化数据
    }

    private static void InjectAllText()
    {
        // TODO: 注入本地化数据到原版
    }

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

        if (_localeShowNames.ContainsKey(locale))
        {
            YuanLogger.LogWarning($"Localization： {locale} 语言重复注册，忽略本次注册");
        }
        else
        {
            _index2Locale[_index2Locale.Count] = locale;
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
        if (!_localeShowNames.ContainsKey(locale))
            throw new ArgumentException("locale 未注册", nameof(locale));

        _fallbackChains[locale] = fallbackChain.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        EnsureNoCycles();
    }

    /// <summary>
    /// 加载指定路径下所有已注册语言的本地化数据：
    /// {path}/locales/{locale}/{namespace}.json
    /// <param name="path">locales文件夹所在路径</param>
    /// </summary>
    public static void LoadFromPath(string path)
    {
        var localesPath = Path.Combine(path, "locales");
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(localesPath))
            throw new DirectoryNotFoundException($"路径不存在：{localesPath}");

        foreach (var locale in _localeShowNames.Keys.ToList())
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
    /// 根据索引获取语言代码，无效索引会返回默认语言代码
    /// </summary>
    /// <param name="index">索引</param>
    /// <returns>语言代码</returns>
    public static string GetLocale(int index)
    {
        return _index2Locale.TryGetValue(index, out var locale) ? locale : DefaultLocale;
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
                    LanguageChanged += SyncLang;
                else
                    LanguageChanged -= SyncLang;
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


    private static void LoadOneFile(string locale, string @namespace, string filePath)
    {
        try
        {
            using var fs = File.OpenRead(filePath);
            using var sr = new StreamReader(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), detectEncodingFromByteOrderMarks: false);
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
}
