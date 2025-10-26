using HarmonyLib;

namespace YuanAPI.LocalizationPatches;

[HarmonyPatch]
public class SetPanelPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SetPanel), nameof(SetPanel.SetLanguage))]
    public static void SetLanguagePostFix()
    {
        YuanLogger.LogDebug("SetLanguagePostFix Called");
        Localization.LanguageChanged?.Invoke(Localization.GetLocale(Mainload.SetData[4]));
    }
}
