﻿// Source code is taken from CommonAPI (GPL-3.0) - https://github.com/limoka/CommonAPI
// Source code is indirectly taken from R2API (MIT) - https://github.com/risk-of-thunder/R2API
#nullable enable
using BepInEx.Logging;
using JetBrains.Annotations;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YuanAPI {

    [Flags]
    internal enum InitStage {
        SetHooks = 1 << 0,
        Load = 1 << 1,
        PostLoad = 1 << 2,
        Unload = 1 << 3,
        UnsetHooks = 1 << 4,
        LoadCheck = 1 << 5,
    }

    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class)]
    internal class YuanAPISubmodule : Attribute {
        public Version? Build;
        public Type[]? Dependencies;
    }

    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    internal class YuanAPISubmoduleInit : Attribute {
        public InitStage Stage;
    }

    /// <summary>
    /// Attribute to have at the top of your BaseUnityPlugin class if you want to load a specific YuanAPI Submodule.
    /// Parameter(s) are the nameof the submodules.
    /// e.g: [YuanAPISubmoduleDependency("", "")]
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class YuanAPISubmoduleDependency : Attribute {
        public string?[]? SubmoduleNames { get; }

        public YuanAPISubmoduleDependency(params string[] submoduleName) {
            SubmoduleNames = submoduleName;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class APISubmoduleHandler {
        private readonly Version _build;
        private readonly ManualLogSource logger;

        private readonly HashSet<string> moduleSet;
        private static readonly HashSet<string> loadedModules;

        private List<Type> allModules;

        static APISubmoduleHandler() {
            loadedModules = new HashSet<string>();
        }

        internal APISubmoduleHandler(Version build, ManualLogSource logger) {
            _build = build;
            this.logger = logger;
            moduleSet = new HashSet<string>();

            allModules = GetSubmodules(true);
        }

        /// <summary>
        /// Return true if the specified submodule is loaded.
        /// </summary>
        /// <param name="submodule">nameof the submodule</param>
        public static bool IsLoaded(string submodule) => loadedModules.Contains(submodule);

        /// <summary>
        /// Load submodule
        /// </summary>
        /// <param name="moduleType">Module type</param>
        /// <returns>Is loading successful?</returns>
        public bool RequestModuleLoad(Type moduleType) {
            if (IsLoaded(moduleType.Name))
                return true;

            logger.LogInfo($"Enabling CommonAPI Submodule: {moduleType.Name}");

            try {
                InvokeStage(moduleType, InitStage.SetHooks, null);
                InvokeStage(moduleType, InitStage.Load, null);
                moduleType.SetFieldValue("_loaded", true);
                loadedModules.Add(moduleType.Name);
                InvokeStage(moduleType, InitStage.PostLoad, null);
                return true;
            } catch (Exception e) {
                logger.LogError($"{moduleType.Name} could not be initialized and has been disabled:\n\n{e.Message}");
            }
            return false;
        }

        internal HashSet<string> LoadRequested(PluginScanner pluginScanner) {

            void AddModuleToSet(IEnumerable<CustomAttributeArgument> arguments) {
                foreach (var arg in arguments) {
                    foreach (var stringElement in (CustomAttributeArgument[])arg.Value) {
                        string moduleName = (string)stringElement.Value;
                        Type moduleType = allModules.First(type => type.Name.Equals(moduleName));

                        IEnumerable<string> modulesToAdd = moduleType.GetDependants(type =>
                        {
                            YuanAPISubmodule attr = type.GetCustomAttribute<YuanAPISubmodule>();
                            return attr.Dependencies ?? Array.Empty<Type>();
                        }, (start, end) =>
                        {
                            logger.LogWarning($"Found Submodule circular dependency! Submodule {start.FullName} depends on {end.FullName}, which depends on {start.FullName}! Submodule {start.FullName} and all of its dependencies will not be loaded.");
                        })
                            .Select(type => type.Name);

                        foreach (string module in modulesToAdd) {
                            moduleSet.Add(module);
                        }
                    }
                }
            }

            void CallWhenAssembliesAreScanned() {
                var moduleTypes = GetSubmodules();

                foreach (var moduleType in moduleTypes) {
                    logger.LogInfo($"Enabling CommonAPI Submodule: {moduleType.Name}");
                }

                var faults = new Dictionary<Type, Exception>();

                moduleTypes.ForEachTry(t => InvokeStage(t, InitStage.SetHooks, null), faults);

                moduleTypes.Where(t => !faults.ContainsKey(t))
                    .ForEachTry(t => InvokeStage(t, InitStage.Load, null), faults);

                moduleTypes.Where(t => !faults.ContainsKey(t))
                    .ForEachTry(t => t.SetFieldValue("_loaded", true));
                moduleTypes.Where(t => !faults.ContainsKey(t))
                    .ForEachTry(t => loadedModules.Add(t.Name));

                moduleTypes.Where(t => !faults.ContainsKey(t))
                    .ForEachTry(t => InvokeStage(t, InitStage.PostLoad, null), faults);

                faults.Keys.ForEachTry(t => {
                    logger.LogError($"{t.Name} could not be initialized and has been disabled:\n\n{faults[t]}");
                    InvokeStage(t, InitStage.UnsetHooks, null);
                });
            }

            var scanRequest = new PluginScanner.AttributeScanRequest(typeof(YuanAPISubmoduleDependency).FullName,
                AttributeTargets.Assembly | AttributeTargets.Class,
                CallWhenAssembliesAreScanned, false,
                (assembly, arguments) =>
                    AddModuleToSet(arguments),
                (type, arguments) =>
                    AddModuleToSet(arguments)
                );

            pluginScanner.AddScanRequest(scanRequest);

            return loadedModules;
        }

        private List<Type> GetSubmodules(bool allSubmodules = false) {
            Type[] types;
            try {
                types = Assembly.GetExecutingAssembly().GetTypes();
            } catch (ReflectionTypeLoadException e) {
                types = e.Types;
            }

            var moduleTypes = types.Where(type => APISubmoduleFilter(type, allSubmodules)).ToList();
            return moduleTypes;
        }

        // ReSharper disable once InconsistentNaming
        private bool APISubmoduleFilter(Type type, bool allSubmodules = false) {
            if (type == null)
                return false;
            var attr = type.GetCustomAttribute<YuanAPISubmodule>();

            if (attr == null)
                return false;

            if (allSubmodules) {
                return true;
            }

            // Comment this out if you want to try every submodules working (or not) state
            if (!moduleSet.Contains(type.Name)) {
                var shouldload = new object[1];
                InvokeStage(type, InitStage.LoadCheck, shouldload);
                if (!(shouldload[0] is bool)) {
                    return false;
                }

                if (!(bool)shouldload[0]) {
                    return false;
                }
            }

            if (attr.Build != default && attr.Build != _build)
                logger.LogDebug($"{type.Name} was built for build {attr.Build}, current build is {_build}.");

            return true;
        }

        internal void InvokeStage(Type type, InitStage stage, object[]? parameters) {
            var method = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(m => m.GetCustomAttributes(typeof(YuanAPISubmoduleInit))
                .Any(a => ((YuanAPISubmoduleInit)a).Stage.HasFlag(stage))).ToList();

            if (method.Count == 0) {
                logger.LogDebug($"{type.Name} has no static method registered for {stage}");
                return;
            }

            method.ForEach(m => m.Invoke(null, parameters));
        }
    }
}
