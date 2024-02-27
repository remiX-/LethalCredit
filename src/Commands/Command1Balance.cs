using LethalCredit.Manager.Saves;
using QualityCompany.Manager.ShipTerminal;

namespace LethalCredit.Commands;

internal class Command1Balance
{
    [TerminalCommand]
    private static TerminalCommandBuilder Balance()
    {
        return new TerminalCommandBuilder("lcu-balance")
            .WithHelpDescription("Check up on your current balance")
            .WithAction(() => $"Your balance is ${SaveManager.SaveData.BankBalance}");
    }
}
