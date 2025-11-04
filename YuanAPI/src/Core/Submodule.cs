#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace YuanAPI;

/// <summary>
/// 将类标记为Submodule<br/>
/// 配合特性 <see cref="AutoInit"/> 使用，可以自动Patch方法<br/>
/// 自动确保Initialize方法只会执行一次 <br/>
/// <param name="UseAutoPatch">启用自动Patch 默认值为true</param>
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
internal class Submodule : Attribute
{
    public bool UseAutoPatch = true;
}

/// <summary>
/// 对标记为 <see cref="Submodule"/> 的类的public method使用 <br/>
/// 使该方法执行前自动调用Initialize方法 <br/>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal class AutoInit : Attribute { }

internal static class SubmoduleManager
{
    private static bool _hasInitialized = false;
    private static Harmony _harmony = new Harmony(YuanAPIPlugin.MODGUID+".Submodule");

    private static Dictionary<Type, Action> _initDelegates = new();
    internal static HashSet<string> HasInitialized = [];

    internal static void Initialize()
    {
        if (_hasInitialized)
            return;

        YuanLogger.LogDebug("Initializing Submodule");

        var assembly = Assembly.GetExecutingAssembly();
        var submoduleTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<Submodule>()?.UseAutoPatch == true)
            .ToList();

        foreach (var type in submoduleTypes)
        {
            var initMethod = type.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static,
                null, Type.EmptyTypes, null);

            if (initMethod != null)
            {
                PatchType(type, initMethod);
            }
            else
            {
                YuanLogger.LogWarning($"SubmoduleManager: {nameof(type)} is submodule but not have Initialize method");
            }
        }

        _hasInitialized = true;
        YuanLogger.LogInfo($"SubmoduleManager: Patched {submoduleTypes.Count} submodule classes");
    }

    private static void PatchType(Type type, MethodInfo initMethod)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => !m.IsSpecialName) // 排除属性访问器等
            .Where(m => m.DeclaringType == type)
            .Where(m => m.GetCustomAttribute<AutoInit>() != null)
            .Where(m => m.Name != "Initialize")
            .ToList();

        var initPrefix = typeof(InitializePatch).GetMethod("InitializePrefix");
        _harmony.Patch(initMethod, prefix: new HarmonyMethod(initPrefix));

        // 创建委托
        _initDelegates[type] = CreateInitDelegate(initMethod);

        foreach (var method in methods)
        {
            // 使用通用补丁
            var patchMethod = typeof(InitializePatch).GetMethod("MethodPrefix");
            _harmony.Patch(method, prefix: new HarmonyMethod(patchMethod));

            YuanLogger.LogDebug($"Patched: {type.Name}.{method.Name}");
        }
    }

    private static Action CreateInitDelegate(MethodInfo initMethod)
    {
        return (Action)Delegate.CreateDelegate(typeof(Action), initMethod);
    }

    public static bool TryGetInitDelegate(Type type, out Action initDelegate)
    {
        return _initDelegates.TryGetValue(type, out initDelegate);
    }
}

// Harmony补丁类
[HarmonyPatch]
public static class InitializePatch
{
    public static bool MethodPrefix(MethodBase __originalMethod)
    {
        if (SubmoduleManager.TryGetInitDelegate(__originalMethod.DeclaringType!, out var initDelegate))
        {
            initDelegate();
        }

        return true;
    }

    public static bool InitializePrefix(MethodBase __originalMethod)
    {
        if (__originalMethod?.DeclaringType == null)
            return true;

        var className = __originalMethod.DeclaringType.FullName;
        return SubmoduleManager.HasInitialized.Add(className);
    }
}
