using System;
using BepInEx;
using HarmonyLib;

namespace YuanAPI;

/// <summary>
/// Plugin class of Yuan API. Entry point
/// </summary>
[BepInPlugin(MODGUID, MODNAME, VERSION)]
public class YuanAPIPlugin : BaseUnityPlugin
{
    public const string MODNAME = "YuanAPI";
    public const string MODGUID = "cc.lymone.HoL." + MODNAME;
    public const string VERSION = "0.1.1";

    internal static Harmony Harmony = new Harmony(MODGUID);

    public static readonly Version BuildFor = new Version(0, 7, 851);
    public static readonly Version Version = Version.Parse(VERSION);

    internal static event Action OnStart;

    private void Awake()
    {
        YuanLogger.SetLogger(new LoggerWrapper(Logger));

        SubmoduleManager.Initialize();
    }

    private void Start()
    {
        OnStart?.Invoke();
    }
}

