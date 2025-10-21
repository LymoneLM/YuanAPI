#nullable enable
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace YuanAPI;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
internal class Submodule : Attribute
{
    public Version? Build;
    public bool AutoPatchPublicMethod = true;
}

[AttributeUsage(AttributeTargets.Method)]
internal class NoNeedLoad : Attribute { }

internal static class SubmoduleManager
{
    private static bool _isInitialized = false;
    private static Harmony _harmony = new Harmony(YuanAPIPlugin.MODGUID+".Submodule");

    private static Dictionary<MethodBase, Action> _hookDelegates = new Dictionary<MethodBase, Action>();
    internal static HashSet<string> HasLoaded = [];

    static SubmoduleManager()
    {
        Initialize();
        YuanLogger.logger.LogInfo($"SubmoduleManager: Initialize done");
    }

    internal static void Initialize()
    {
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
                YuanLogger.logger.LogWarning($"SubmoduleManager: {nameof(type)} is submodule but not have SetHooks method");
            }
        }

        _isInitialized = true;
        YuanLogger.logger.LogInfo($"SubmoduleManager: Patched {submoduleTypes.Count} submodule classes");
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

        foreach (var method in methods)
        {
            // 创建高性能委托
            var hookDelegate = CreateHookDelegate(setHooksMethod);
            _hookDelegates[method] = hookDelegate;

            // 使用通用补丁
            var patchMethod = typeof(SetHooksPatch).GetMethod("MethodPrefix");
            _harmony.Patch(method, prefix: new HarmonyMethod(patchMethod));

            YuanLogger.logger.LogDebug($"Patched: {type.Name}.{method.Name}");
        }
    }

    private static Action CreateHookDelegate(MethodInfo setHooksMethod)
    {
        // 使用DynamicMethod创建直接调用SetHook的委托
        var dynamicMethod = new DynamicMethod(
            "DirectSetHookCall",
            null,
            Type.EmptyTypes,
            typeof(SubmoduleManager).Module,
            true
        );

        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Call, setHooksMethod);
        il.Emit(OpCodes.Ret);

        return (Action)dynamicMethod.CreateDelegate(typeof(Action));
    }

    public static bool TryGetHookDelegate(MethodBase method, out Action hookDelegate)
    {
        return _hookDelegates.TryGetValue(method, out hookDelegate);
    }
}

// Harmony补丁类
[HarmonyPatch]
public static class SetHooksPatch
{
    public static bool MethodPrefix(MethodBase __originalMethod)
    {
        if (SubmoduleManager.TryGetHookDelegate(__originalMethod!, out var hookDelegate))
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
