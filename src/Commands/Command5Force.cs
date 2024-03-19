using LethalCredit.Manager.Bank;
using LethalCredit.Manager.Saves;
using QualityCompany.Manager.ShipTerminal;
using System;
using Unity.Netcode;

namespace LethalCredit.Commands;

internal class Command5Force
{
    private static int _value;

    [TerminalCommand]
    private static TerminalCommandBuilder Force()
    {
        return new TerminalCommandBuilder("lcu-force")
            .WithHelpDescription("Force update LCU's bank balance. Note: This should be used for beta testing purposes and it is limited to the host.")
            .WithSubCommand(CreateSubCommand())
            .AddTextReplacement("[bankBalance]", () => $"${SaveManager.SaveData.BankBalance}")
            .WithCondition("isHost", "You are not host.", () => NetworkManager.Singleton.IsHost);
    }

    private static TerminalSubCommandBuilder CreateSubCommand()
    {
        return new TerminalSubCommandBuilder("<amount>")
            .WithDescription("BETA TESTING PURPOSES ONLY\n\nForce the bank to have a specific amount.")
            .WithMessage("Your new balance: [bankBalance]")
            .WithConditions("isHost")
            .WithInputMatch(@"^(\d+)$")
            .WithPreAction(input =>
            {
                if (!NetworkManager.Singleton.IsHost) return "not host";

                _value = Convert.ToInt32(input);

                if (_value < 0) _value = 0;

                return null;
            })
            .WithAction(input =>
            {
                _value = Convert.ToInt32(input);

                if (_value < 0) _value = 0;
                BankNetworkHandler.Instance.SyncBankBalanceClientRpc(_value);
            });
    }
}
