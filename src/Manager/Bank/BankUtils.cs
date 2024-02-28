namespace LethalCredit.Manager.Bank;

internal class BankUtils
{
    internal static int GetBankAmountForCredits(int creditsAmount)
    {
        var multiplyBy = (float)Plugin.Instance.PluginConfig.BankCreditsRatePercentage / 100;
        return (int)(creditsAmount * multiplyBy);
    }
}
