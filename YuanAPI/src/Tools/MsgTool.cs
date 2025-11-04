using System.Collections.Generic;

namespace YuanAPI;

public enum TipLv
{
    Info,
    Warning,
    ShortInfo,
}
public static class MsgTool
{
    /// <summary>
    /// 在游戏画面上方显示一个提示信息
    /// </summary>
    /// <param name="msg">消息内容</param>
    /// <param name="lv">消息类型</param>
    public static void TipMsg(string msg, TipLv lv = TipLv.Info)
    {
        if (!string.IsNullOrEmpty(msg))
            Mainload.Tip_Show.Add([((int)lv).ToString(), msg]);
    }
}
