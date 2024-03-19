using LethalCredit.Manager.Bank;
using LethalCredit.Manager.Saves;
using QualityCompany.Manager.ShipTerminal;
using QualityCompany.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalCredit.Commands;

internal class Command3Withdraw
{
    private static List<GrabbableObject> _recommendedScraps = new();
    private static int _valueFor;
    private static int _newBalance;

    [TerminalCommand]
    private static TerminalCommandBuilder Withdraw()
    {
        return new TerminalCommandBuilder("lcu-withdraw")
            .WithHelpDescription("Withdraw some money from The Lethal Credit Union Bank")
            .WithSubCommand(CreateWithdrawAllCommand())
            .WithSubCommand(CreateWithdrawQuotaCommand())
            .WithSubCommand(CreateWithdrawSubCommand())
            .AddTextReplacement("[valueFor]", () => $"${_valueFor}")
            .AddTextReplacement("[depositActualTotal]", () => $"${_recommendedScraps.ScrapValueOfCollection()}")
            .AddTextReplacement("[depositScrapCombo]", GenerateDepositScrapComboText)
            .AddTextReplacement("[numScrapDeposited]", () => _recommendedScraps.Count)
            .AddTextReplacement("[newBalance]", () => $"${_newBalance}")
            .AddTextReplacement("[bankBalance]", () => $"${SaveManager.SaveData.BankBalance}")
            .WithCondition("hasEnoughBalance", "You do not have enough money in the bank.\n\nCurrent balance: [bankBalance].", () => SaveManager.SaveData.BankBalance >= _valueFor);

        static string GenerateDepositScrapComboText()
        {
            if (_recommendedScraps is null || _recommendedScraps.Count == 0) return "No items";

            return _recommendedScraps
                .Select(x => $"{x.itemProperties.name}: {x.scrapValue}")
                .Aggregate((first, next) => $"{first}\n{next}");
        }
    }

    private static TerminalSubCommandBuilder CreateWithdrawAllCommand()
    {
        return new TerminalSubCommandBuilder("all")
            .WithDescription("Withdraw all LCU bucks")
            .WithMessage("Withdrawing [valueFor].\n\nYour new balance would be [newBalance]")
            .EnableConfirmDeny(confirmMessage: "Withdrew [valueFor].\n\nYour balance: [newBalance]")
            .WithConditions("hasEnoughBalance")
            .WithPreAction(() =>
            {
                _valueFor = SaveManager.SaveData.BankBalance;
                _newBalance = 0;

                return null;
            })
            .WithAction(() =>
            {
                BankNetworkHandler.Instance.WithdrawServerRpc(_valueFor, GameNetworkManager.Instance.localPlayerController.playerClientId);
            });
    }

    private static TerminalSubCommandBuilder CreateWithdrawQuotaCommand()
    {
        return new TerminalSubCommandBuilder("quota")
            .WithDescription("Withdraw LCU bucks equivalent to current quota")
            .WithMessage("Withdrawing [valueFor].\n\nYour new balance would be [newBalance]")
            .EnableConfirmDeny(confirmMessage: "Withdrew [valueFor].\n\nYour balance: [newBalance]")
            .WithConditions("hasEnoughBalance")
            .WithPreAction(() =>
            {
                _valueFor = TimeOfDay.Instance.profitQuota - TimeOfDay.Instance.quotaFulfilled;
                _newBalance = SaveManager.SaveData.BankBalance - _valueFor;

                return null;
            })
            .WithAction(() =>
            {
                BankNetworkHandler.Instance.WithdrawServerRpc(_valueFor, GameNetworkManager.Instance.localPlayerController.playerClientId);
            });
    }

    private static TerminalSubCommandBuilder CreateWithdrawSubCommand()
    {
        return new TerminalSubCommandBuilder("<amount>")
            .WithDescription("Withdraw a specific amount of LCU bucks")
            .WithMessage("Withdrawing [valueFor].\n\nYour new balance would be [newBalance]")
            .EnableConfirmDeny(confirmMessage: "Withdrew [valueFor].\n\nYour balance: [newBalance]")
            .WithConditions("hasEnoughBalance")
            .WithInputMatch(@"^(\d+)$")
            .WithPreAction(input =>
            {
                _valueFor = Convert.ToInt32(input);
                if (_valueFor <= 0) return "posiotive";

                _newBalance = SaveManager.SaveData.BankBalance - _valueFor;

                return null;
            })
            .WithAction(() =>
            {
                BankNetworkHandler.Instance.WithdrawServerRpc(_valueFor, GameNetworkManager.Instance.localPlayerController.playerClientId);
            });
    }
}
