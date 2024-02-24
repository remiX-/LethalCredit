using LethalCredit.Manager.Bank;
using LethalCredit.Manager.Saves;
using QualityCompany.Manager.ShipTerminal;
using QualityCompany.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalCredit.Commands;
internal class DepositCommand
{
    private static List<GrabbableObject> _recommendedScraps = new();
    private static int _valueFor;

    [TerminalCommand]
    private static TerminalCommandBuilder Deposit()
    {
        return new TerminalCommandBuilder("lc.deposit")
            .WithSubCommand(CreateDepositSubCommand())
            .AddTextReplacement("[valueFor]", () => $"${_valueFor}")
            .AddTextReplacement("[depositActualTotal]", () => $"${_recommendedScraps.ScrapValueOfCollection()}")
            .AddTextReplacement("[depositScrapCombo]", GenerateDepositScrapComboText)
            .AddTextReplacement("[numScrapDeposited]", () => _recommendedScraps.Count)
            .AddTextReplacement("[newBalance]", () => $"${SaveManager.SaveData.BankBalance - _valueFor}")
            .AddTextReplacement("[bankBalance]", () => $"${SaveManager.SaveData.BankBalance}")
            .WithCondition("hasScrapItems", "Bruh, you don't even have any items.", () => ScrapUtils.GetAllScrapInShip().Count > 0)
            .WithCondition("notEnoughScrap", "Not enough scrap to meet [sellScrapFor] credits.\nTotal value: [sellScrapActualTotal].", () => _valueFor < ScrapUtils.GetShipTotalSellableScrapValue());

        static string GenerateDepositScrapComboText()
        {
            if (_recommendedScraps is null || _recommendedScraps.Count == 0) return "No items";

            return _recommendedScraps
                .Select(x => $"{x.itemProperties.name}: {x.scrapValue}")
                .Aggregate((first, next) => $"{first}\n{next}");
        }
    }

    [TerminalCommand]
    private static TerminalCommandBuilder Withdraw()
    {
        return new TerminalCommandBuilder("lc.withdraw")
            .WithSubCommand(CreateWithdrawSubCommand())
            .AddTextReplacement("[valueFor]", () => $"${_valueFor}")
            .AddTextReplacement("[depositActualTotal]", () => $"${_recommendedScraps.ScrapValueOfCollection()}")
            .AddTextReplacement("[depositScrapCombo]", GenerateDepositScrapComboText)
            .AddTextReplacement("[numScrapDeposited]", () => _recommendedScraps.Count)
            .WithCondition("hasEnoughBalance", "You do not have enough moneh, current balance: [bankBalance]. git gud", () => SaveManager.SaveData.BankBalance >= _valueFor);

        static string GenerateDepositScrapComboText()
        {
            if (_recommendedScraps is null || _recommendedScraps.Count == 0) return "No items";

            return _recommendedScraps
                .Select(x => $"{x.itemProperties.name}: {x.scrapValue}")
                .Aggregate((first, next) => $"{first}\n{next}");
        }
    }

    [TerminalCommand]
    private static TerminalCommandBuilder Balance()
    {
        return new TerminalCommandBuilder("lc.balance")
            .WithAction(() => $"Your balance is ${SaveManager.SaveData.BankBalance}");
    }

    private static TerminalSubCommandBuilder CreateDepositSubCommand()
    {
        return new TerminalSubCommandBuilder("<amount>")
            .WithDescription("Deposit as close as possible to input amount")
            .WithMessage("Requesting to deposit value close to [valueFor].\n\nLethal Credit Union will accept the following items for a total of [depositActualTotal]:\n[depositScrapCombo]")
            .EnableConfirmDeny(confirmMessage: "Deposited [numScrapDeposited] scrap items for [depositActualTotal].")
            .WithConditions("hasScrapItems", "notEnoughScrap")
            .WithInputMatch(@"^(\d+)$")
            .WithPreAction(input =>
            {
                _valueFor = Convert.ToInt32(input);

                if (_valueFor <= 0) return false;

                _recommendedScraps = ScrapUtils.GetScrapForAmount(_valueFor)
                    .OrderBy(x => x.itemProperties.name)
                    .ThenByDescending(x => x.scrapValue)
                    .ToList();

                if (_recommendedScraps.Count == 0) return false;

                return true;
            })
            .WithAction(() =>
            {
                BankNetworkHandler.Instance.DepositServerRpc(_recommendedScraps.Select(x => x.NetworkObjectId).ToArray());
            });
    }

    private static TerminalSubCommandBuilder CreateWithdrawSubCommand()
    {
        return new TerminalSubCommandBuilder("<amount>")
            .WithDescription("Withdraw a specific amount from Lethal Credit Union")
            .WithMessage("Withdrawing [valueFor].\n\nYour new balance would be [newBalance]")
            .EnableConfirmDeny(confirmMessage: "Withdrew [valueFor].\n\nYour balance: [bankBalance]")
            .WithConditions("hasEnoughBalance")
            .WithInputMatch(@"^(\d+)$")
            .WithPreAction(input =>
            {
                _valueFor = Convert.ToInt32(input);

                return true;
            })
            .WithAction(() =>
            {
                BankNetworkHandler.Instance.WithdrawServerRpc(_valueFor);
            });
    }
}
