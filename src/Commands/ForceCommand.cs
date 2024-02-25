using LethalCredit.Manager.Bank;
using LethalCredit.Manager.Saves;
using QualityCompany.Manager.ShipTerminal;
using System;
using Unity.Netcode;

namespace LethalCredit.Commands;

internal class ForceCommand
{
    private static int _value;

    [TerminalCommand]
    private static TerminalCommandBuilder Force()
    {
        return new TerminalCommandBuilder("lcu force")
            .WithSubCommand(CreateSubCommand())
            .AddTextReplacement("[f_bankBalance]", () => $"${SaveManager.SaveData.BankBalance}")
            .WithCondition("isHost", "You are not host.", () => NetworkManager.Singleton.IsHost);
    }

    private static TerminalSubCommandBuilder CreateSubCommand()
    {
        return new TerminalSubCommandBuilder("<amount>")
            .WithDescription("BETA TESTING PURPOSES ONLY\n\nForce the bank to have a specific amount.")
            .WithMessage("Your new balance: [f_bankBalance]")
            .WithInputMatch(@"^(\d+)$")
            .WithPreAction(input =>
            {
                _value = Convert.ToInt32(input);

                if (_value <= 0) return false;

                BankNetworkHandler.Instance.SyncBankBalanceClientRpc(_value);
                return true;
            })
            .WithAction(() =>
            {
                // sorry
            });
    }
}
