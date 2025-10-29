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
        LocalizationRegistry.CallLanguageChanged(LocalizationRegistry.GetLocale(Mainload.SetData[4]));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SetPanel), "InitShow")]
    public static void LoadLanguageOption(SetPanel __instance)
    {
        var langDropdown = __instance.transform.Find("LoadSpeed").Find("AllClass").GetComponent<Dropdown>();
        langDropdown.options =
            LocalizationRegistry.GetAllShowNames().Select(name => new Dropdown.OptionData(name)).ToList();
        langDropdown.value = Mainload.SetData[4];
    }
}
