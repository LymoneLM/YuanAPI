using System;
using System.Collections.Generic;

namespace YuanAPI;

public class PropData
{
    public string ID { get; set; }
    public int Price { get; set; } = 0;
    public int Category { get; set; } = (int)PropCategory.ModDefault;
    public Dictionary<int, int> PropEffect { get; set; } = [];

    public List<string> Text { get; set; } = [];
    public string PrefabPath { get; set; }

    public PropData() { }

    public PropData(string id, int price = 0, int category = (int)PropCategory.ModDefault,
                    Dictionary<int, int> propEffect = null,
                    List<string> text = null, string prefabPath = null)
    {
        this.ID = id;
        this.Price = price;
        this.Category = category;
        this.PropEffect = propEffect ?? new Dictionary<int, int>();
        Text = text ?? [];
        this.PrefabPath = prefabPath;
    }

    public bool IsValid()
    {
        var isValid = !string.IsNullOrEmpty(this.ID);
        if ( Text.Count == 0 || string.IsNullOrEmpty(PrefabPath) )
            isValid = false;
        return isValid;
    }

    public List<string> ToVanillaPropDataList()
    {
        if (!IsValid())
            throw new PropException("无法使用非法数据构造数据序列");

        var result = new List<string>();

        result.Add(Price.ToString());
        result.Add(Category.ToString());
        result.Add(GetEffectString());

        return result;
    }

    public string GetEffectString()
    {
        List<string> effectsStr = [];
        foreach (PropEffectType effect in Enum.GetValues(typeof(PropEffectType)))
        {
            effectsStr.Add(PropEffect.ContainsKey((int)effect) ? PropEffect[(int)effect].ToString() : 0.ToString());
        }
        return string.Join("|", effectsStr);
    }

}
