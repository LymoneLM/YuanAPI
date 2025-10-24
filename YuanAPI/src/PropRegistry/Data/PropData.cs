using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YuanAPI;

public class PropData
{
    public string ID { get; set; }
    public int Price { get; set; }
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

    public static PropData FromVanillaPropData(List<string> listData, int index)
    {
        return new PropData(index.ToString(),
                            int.Parse(listData[0]),
                            int.Parse(listData[1]),
                            listData[2].Split('|')
                                .Select((str,effect)=>(effect, int.Parse(str)))
                                .ToDictionary(x=>x.effect,x=>x.Item2),
                            ["中文","English"],        //TODO: 改用L10N
                            $"AllProp/{index}");
    }

    public List<string> ToVanillaPropData()
    {
        if (!IsValid())
            throw new InvalidDataException("无法使用非法数据构造数据序列");

        var result = new List<string>
        {
            Price.ToString(),
            Category.ToString(),
            GetEffectString()
        };

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
