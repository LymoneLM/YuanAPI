using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine.UI;

namespace YuanAPI.LocalizationPatches;

[HarmonyPatch]
public class SetPanelPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SetPanel), nameof(SetPanel.SetLanguage))]
    public static void SetLanguagePostFix()
    {
        Localization.CallLanguageChanged(Localization.GetLocale(Mainload.SetData[4]));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SetPanel), "InitShow")]
    public static void LoadLanguageOption(SetPanel __instance)
    {
        var langDropdown = __instance.transform.Find("Language").Find("AllClass").GetComponent<Dropdown>();
        langDropdown.options =
            Localization.GetAllShowNames().Select(name => new Dropdown.OptionData(name)).ToList();
        langDropdown.value = Mainload.SetData[4];
    }
}
