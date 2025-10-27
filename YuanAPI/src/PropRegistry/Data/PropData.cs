using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YuanAPI;

public class PropData
{
    public string PropNamespace { get; set; } = PropRegistry.DefaultNamespace;
    public string PropID { get; set; }
    public string Uid => $"{PropNamespace}:{PropID}";

    public int? Price { get; set; } = null;
    public int? Category { get; set; } = null;
    public Dictionary<int, int> PropEffect { get; set; } = new();

    public string TextNamespace { get; set; } = Localization.DefaultNamespace;
    public string TextKey { get; set; }
    public string PrefabPath { get; set; }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(PropNamespace) && !string.IsNullOrEmpty(PropID) &&
               !string.IsNullOrEmpty(TextNamespace) && !string.IsNullOrEmpty(TextKey) &&
               !string.IsNullOrEmpty(PrefabPath) &&
               Price.HasValue && Category.HasValue;
    }

    internal static PropData FromVanillaPropData(List<string> listData, int index)
    {
        return new PropData
        {
            PropNamespace = PropRegistry.VanillaNamespace,
            PropID = index.ToString(),
            Price = int.Parse(listData[0]),
            Category = int.Parse(listData[1]),
            PropEffect = listData[2].Split('|')
                .Select((str, effect) => (effect, int.Parse(str)))
                .ToDictionary(x => x.effect, x => x.Item2),
            TextNamespace = Localization.VanillaNamespace,
            TextKey = $"Prop.{index}",
            PrefabPath = $"AllProp/{index}",
        };
    }

    internal List<string> ToVanillaPropData()
    {
        if (!IsValid())
            throw new InvalidDataException("无法使用非法数据构造数据序列");

        var result = new List<string>
        {
            Price.ToString(),
            Category.ToString(),
            string.Join("|",
                from PropEffectType effect in Enum.GetValues(typeof(PropEffectType))
                select PropEffect.TryGetValue((int)effect, out var value) ? value.ToString() : "0")
        };

        return result;
    }

    public static PropData operator +(PropData sample, PropData that)
    {
        if (sample == null) return that;
        if (that == null) return sample;

        var result = new PropData
        {
            PropNamespace = that.PropNamespace != PropRegistry.DefaultNamespace ?
                that.PropNamespace : sample.PropNamespace,

            PropID = !string.IsNullOrEmpty(that.PropID) ? that.PropID : sample.PropID,

            Price = that.Price ?? sample.Price,

            Category = that.Category ?? sample.Category,

            TextNamespace = that.TextNamespace != Localization.DefaultNamespace ?
                that.TextNamespace : sample.TextNamespace,

            TextKey = !string.IsNullOrEmpty(that.TextKey) ? that.TextKey : sample.TextKey,

            PrefabPath = !string.IsNullOrEmpty(that.PrefabPath) ? that.PrefabPath : sample.PrefabPath,

            PropEffect = new Dictionary<int, int>()
        };

        foreach (var effect in sample.PropEffect)
        {
            result.PropEffect[effect.Key] = effect.Value;
        }

        foreach (var effect in that.PropEffect)
        {
            result.PropEffect[effect.Key] = effect.Value;
        }

        return result;
    }
}
