using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace YuanAPI;

/// <summary>
/// Plugin class of Yuan API. Entry point
/// </summary>
[BepInPlugin(MODGUID, MODNAME, VERSION)]
public class YuanAPI : BaseUnityPlugin
{
    public const string MODNAME = "YuanAPI";
    public const string MODGUID = "cc.lymone.HoL." + MODNAME;
    public const string VERSION = "0.1.0";

    internal static Harmony harmony = new Harmony(MODGUID);
    internal static IYuanLogger logger;

    public static readonly Version buildFor = new Version(0, 7, 851);
    public static readonly Version version = Version.Parse(VERSION);

    private void Awake()
    {
        logger = new LoggerWrapper(Logger);
        YuanLogger.SetLogger(logger);


    }
}

