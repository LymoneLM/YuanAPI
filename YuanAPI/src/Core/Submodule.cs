#nullable enable
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YuanAPI;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
internal class Submodule : Attribute
{
    public Version? Build = null;
    public bool AutoPatchPublicMethod = true;
}

[AttributeUsage(AttributeTargets.Method)]
internal class NoNeedLoad : Attribute { }

internal static class SubmoduleManager
{
    private static bool _isInitialized = false;
    private static Harmony _harmony = new Harmony(YuanAPIPlugin.MODGUID+".Submodule");

    private static Dictionary<Type, Action> _hookDelegates = new Dictionary<Type, Action>();
    internal static HashSet<string> HasLoaded = [];

    internal static void Initialize()
    {
        YuanLogger.LogDebug("Initializing Submodule");
        if (_isInitialized)
            return;

        var assembly = Assembly.GetExecutingAssembly();
        var submoduleTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<Submodule>()?.AutoPatchPublicMethod == true)
            .ToList();

        foreach (var type in submoduleTypes)
        {
            var setHooksMethod = type.GetMethod("SetHooks", BindingFlags.Public | BindingFlags.Static,
                null, Type.EmptyTypes, null);

            if (setHooksMethod != null)
            {
                PatchType(type, setHooksMethod);
            }
            else
            {
                YuanLogger.LogWarning($"SubmoduleManager: {nameof(type)} is submodule but not have SetHooks method");
            }
        }

        _isInitialized = true;
        YuanLogger.LogInfo($"SubmoduleManager: Patched {submoduleTypes.Count} submodule classes");
    }

    private static void PatchType(Type type, MethodInfo setHooksMethod)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => !m.IsSpecialName) // 排除属性访问器等
            .Where(m => m.DeclaringType == type)
            .Where(m => m.GetCustomAttribute<NoNeedLoad>() == null)
            .Where(m => m.Name != "SetHooks")
            .ToList();

        var setHooksPrefix = typeof(SetHooksPatch).GetMethod("SetHooksPrefix");
        _harmony.Patch(setHooksMethod, prefix: new HarmonyMethod(setHooksPrefix));

        var setHooksPostfix = typeof(SetHooksPatch).GetMethod("SetHooksPostfix");
        _harmony.Patch(setHooksMethod, postfix: new HarmonyMethod(setHooksPostfix));

        // 创建委托
        _hookDelegates[type] = CreateHookDelegate(setHooksMethod);

        foreach (var method in methods)
        {
            // 使用通用补丁
            var patchMethod = typeof(SetHooksPatch).GetMethod("MethodPrefix");
            _harmony.Patch(method, prefix: new HarmonyMethod(patchMethod));

            YuanLogger.LogDebug($"Patched: {type.Name}.{method.Name}");
        }
    }

    private static Action CreateHookDelegate(MethodInfo setHooksMethod)
    {
        return (Action)Delegate.CreateDelegate(typeof(Action), setHooksMethod);
    }

    public static bool TryGetHookDelegate(Type type, out Action hookDelegate)
    {
        return _hookDelegates.TryGetValue(type, out hookDelegate);
    }
}

// Harmony补丁类
[HarmonyPatch]
public static class SetHooksPatch
{
    public static bool MethodPrefix(MethodBase __originalMethod)
    {
        if (SubmoduleManager.TryGetHookDelegate(__originalMethod.DeclaringType!, out var hookDelegate))
        {
            hookDelegate();
        }

        return true;
    }

    public static bool SetHooksPrefix(MethodBase __originalMethod)
    {
        if (__originalMethod?.DeclaringType == null)
            return true;

        var className = __originalMethod.DeclaringType.FullName;
        return !SubmoduleManager.HasLoaded.Contains(className);
    }

    public static void SetHooksPostfix(MethodBase __originalMethod)
    {
        if (__originalMethod?.DeclaringType == null)
            return;

        var className = __originalMethod.DeclaringType.FullName;
        SubmoduleManager.HasLoaded.Add(className);
    }
}
