#nullable enable
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YuanAPI;

/// <summary>
/// 自动Patch所有public方法，使其调用前先执行Initialize方法
/// 并确保Initialize方法只会执行一次
/// <param name="AutoPatchPublicMethod">启用自动Patch</param>
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
internal class Submodule : Attribute
{
    public bool AutoPatchPublicMethod = true;
}

[AttributeUsage(AttributeTargets.Method)]
internal class NoInit : Attribute { }

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
            .Where(t => t.GetCustomAttribute<Submodule>()?.AutoPatchPublicMethod == true)
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
            .Where(m => m.GetCustomAttribute<NoInit>() == null)
            .Where(m => m.Name != "Initialize")
            .ToList();

        var initPrefix = typeof(InitializePatch).GetMethod("InitializePrefix");
        _harmony.Patch(initMethod, prefix: new HarmonyMethod(initPrefix));

        var initPostfix = typeof(InitializePatch).GetMethod("InitializePostfix");
        _harmony.Patch(initMethod, postfix: new HarmonyMethod(initPostfix));

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
        return !SubmoduleManager.HasInitialized.Contains(className);
    }

    public static void InitializePostfix(MethodBase __originalMethod)
    {
        if (__originalMethod?.DeclaringType == null)
            return;

        var className = __originalMethod.DeclaringType.FullName;
        SubmoduleManager.HasInitialized.Add(className);
    }
}
