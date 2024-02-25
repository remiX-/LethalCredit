using LethalCredit.Manager.Saves;
using QualityCompany.Manager.ShipTerminal;

namespace LethalCredit.Commands;

internal class BalanceCommand
{
    [TerminalCommand]
    private static TerminalCommandBuilder Balance()
    {
        return new TerminalCommandBuilder("lcu balance")
            .WithAction(() => $"Your balance is ${SaveManager.SaveData.BankBalance}");
    }
}
