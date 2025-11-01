using System;

namespace YuanAPI;

public static class PropTool
{
    /// <summary>
    /// 向库存中添加物品
    /// </summary>
    /// <param name="propId">物品ID</param>
    /// <param name="propCount">物品数量</param>
    /// <param name="storage">是否占用存储空间</param>
    /// <param name="silence">是否禁用失败提示消息</param>
    /// <returns>是否添加成功</returns>
    public static bool AddProp(int propId, int propCount, bool storage = true, bool silence = false)
    {
        // ID合法
        if (propId < 0 || propId > Mainload.AllPropdata.Count)
        {
            return false;
        }

        // 检查存储空间
        int.TryParse(Mainload.FamilyData[5], out var storageLastSpace);
        if (storage)
        {
            if (storageLastSpace - propCount < 0)
            {
                if (!silence)
                    // 库房容量已满，请在府邸建造或升级库房！
                    MsgTool.TipMsg(AllText.Text_TipShow[21][(int)Mainload.SetData[4]], TipLv.Warning);
                return false;
            }
            Mainload.FamilyData[5] = (storageLastSpace - propCount).ToString();
        }

        // 添加物品
        var propIdStr = propId.ToString();
        var count = Mainload.Prop_have.Count;
        for (var i = 0; i < count; i++)
        {
            if (Mainload.Prop_have[i][0] != propIdStr)
                continue;

            Mainload.Prop_have[i][1] =
                (int.Parse(Mainload.Prop_have[i][1]) + propCount).ToString();
            return true;
        }
        Mainload.Prop_have.Add([propIdStr, propCount.ToString()]);

        return true;
    }

    /// <summary>
    /// 向库存中添加物品
    /// </summary>
    /// <param name="propId">物品ID</param>
    /// <param name="propCount">物品数量</param>
    /// <param name="storage">是否占用存储空间</param>
    /// <param name="silence">是否禁用失败提示消息</param>
    /// <returns>是否添加成功</returns>
    public static bool AddProp(string propId, int propCount, bool storage = true, bool silence = false)
        => AddProp(int.Parse(propId), propCount, storage, silence);
}
