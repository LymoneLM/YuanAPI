namespace YuanAPI;

public class CoinTool
{
    public bool TryChangeCoins(int count)
    {
        if (count < 0 && FormulaData.GetCoinsNum() < -count)
        {
            return false;
        }
        FormulaData.ChangeCoins(count);
        return true;
    }
}
