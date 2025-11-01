using HarmonyLib;

namespace YuanAPI.LocalizationPatches;

[HarmonyPatch]
public class SaveDataPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveData), nameof(SaveData.ReadSetData))]
    public static void LanguageLimitPatch()
    {
        var setData = ES3.Load("SetData", "FW/SetData.es3", Mainload.SetData);
        Mainload.SetData[4] = setData[4] < Localization.LocaleCount() ? setData[4] : 0;
    }
}
