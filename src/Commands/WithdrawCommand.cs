using LethalCredit.Manager.Bank;
using LethalCredit.Manager.Saves;
using QualityCompany.Manager.ShipTerminal;
using QualityCompany.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalCredit.Commands;

internal class WithdrawCommand
{
    private static List<GrabbableObject> _recommendedScraps = new();
    private static int _valueFor;
    private static int _newBalance;

    [TerminalCommand]
    private static TerminalCommandBuilder Withdraw()
    {
        return new TerminalCommandBuilder("lcu withdraw")
            .WithSubCommand(CreateWithdrawSubCommand())
            .AddTextReplacement("[wd_valueFor]", () => $"${_valueFor}")
            .AddTextReplacement("[depositActualTotal]", () => $"${_recommendedScraps.ScrapValueOfCollection()}")
            .AddTextReplacement("[depositScrapCombo]", GenerateDepositScrapComboText)
            .AddTextReplacement("[numScrapDeposited]", () => _recommendedScraps.Count)
            .AddTextReplacement("[wd_newBalance]", () => $"${_newBalance}")
            .WithCondition("hasEnoughBalance", "You do not have enough moneh, current balance: [bankBalance]. git gud", () => SaveManager.SaveData.BankBalance >= _valueFor);

        static string GenerateDepositScrapComboText()
        {
            if (_recommendedScraps is null || _recommendedScraps.Count == 0) return "No items";

            return _recommendedScraps
                .Select(x => $"{x.itemProperties.name}: {x.scrapValue}")
                .Aggregate((first, next) => $"{first}\n{next}");
        }
    }

    private static TerminalSubCommandBuilder CreateWithdrawSubCommand()
    {
        return new TerminalSubCommandBuilder("<amount>")
            .WithDescription("Withdraw a specific amount from Lethal Credit Union")
            .WithMessage("Withdrawing [c].\n\nYour new balance would be [wd_newBalance]")
            .EnableConfirmDeny(confirmMessage: "Withdrew [wd_valueFor].\n\nYour balance: [wd_newBalance]")
            .WithConditions("hasEnoughBalance")
            .WithInputMatch(@"^(\d+)$")
            .WithPreAction(input =>
            {
                _valueFor = Convert.ToInt32(input);
                if (_valueFor <= 0) return false;

                _newBalance = SaveManager.SaveData.BankBalance - _valueFor;

                return true;
            })
            .WithAction(() =>
            {
                BankNetworkHandler.Instance.WithdrawServerRpc(_valueFor);
            });
    }
}
