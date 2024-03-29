﻿using LethalCredit.Manager.Bank;
using LethalCredit.Manager.Saves;
using QualityCompany.Manager.ShipTerminal;
using QualityCompany.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalCredit.Commands;

internal class Command2Deposit
{
    private static List<GrabbableObject> _recommendedScraps = new();
    private static int _valueFor;

    [TerminalCommand]
    private static TerminalCommandBuilder Deposit()
    {
        return new TerminalCommandBuilder("lcu-deposit")
            .WithHelpDescription("Let LCU gobble up your scrap to a safe place")
            .WithSubCommand(CreateDepositAllCommand())
            .WithSubCommand(CreateDepositSubCommand())
            .AddTextReplacement("[valueFor]", () => $"${_valueFor}")
            .AddTextReplacement("[depositActualTotal]", () => $"${_recommendedScraps.ScrapValueOfCollection()}")
            .AddTextReplacement("[depositScrapCombo]", GenerateDepositScrapComboText)
            .AddTextReplacement("[numScrapDeposited]", () => _recommendedScraps.Count)
            .AddTextReplacement("[newBalance]", () => $"${SaveManager.SaveData.BankBalance - _valueFor}")
            .AddTextReplacement("[bankBalance]", () => $"${SaveManager.SaveData.BankBalance}")
            .AddTextReplacement("[shipActualTotal]", () => $"${ScrapUtils.GetShipTotalIncludedScrapValue(Plugin.Instance.PluginConfig.BankIgnoreList)}")
            .WithCondition("d_hasScrapItems", "You do not have any scrap available to for deposit.",
                () => ScrapUtils.GetAllIncludedScrapInShip(Plugin.Instance.PluginConfig.BankIgnoreList).Count > 0)
            .WithCondition("depositMoreThanZero", "Do you really think you can deposit nothing?", () => _valueFor > 0)
            .WithCondition("notEnoughScrap",
                "LCU is not happy with this. You do not enough scrap to deposit [valueFor].\n\nYour currently have a total of [shipActualTotal].",
                () => _valueFor < ScrapUtils.GetShipTotalIncludedScrapValue(Plugin.Instance.PluginConfig.BankIgnoreList));

        static string GenerateDepositScrapComboText()
        {
            if (_recommendedScraps is null || _recommendedScraps.Count == 0) return "No items";

            return _recommendedScraps
                .Select(x => $"{x.itemProperties.name}: {x.scrapValue}")
                .Aggregate((first, next) => $"{first}\n{next}");
        }
    }

    private static TerminalSubCommandBuilder CreateDepositAllCommand()
    {
        return new TerminalSubCommandBuilder("all")
            .WithDescription("Deposit all available scrap in the ship.")
            .WithMessage("Requesting to deposit ALL scrap.\n\nLethal Credit Union will accept the following items for a total of [depositActualTotal]:\n[depositScrapCombo]")
            .EnableConfirmDeny(confirmMessage: "Deposited [numScrapDeposited] scrap items for [depositActualTotal].")
            .WithConditions("d_hasScrapItems")
            .WithPreAction(() =>
            {
                _recommendedScraps = ScrapUtils.GetAllIncludedScrapInShip(Plugin.Instance.PluginConfig.BankIgnoreList);
                _valueFor = _recommendedScraps.ActualScrapValueOfCollection();
            })
            .WithAction(() =>
            {
                BankNetworkHandler.Instance.DepositServerRpc(_recommendedScraps.Select(x => x.NetworkObjectId).ToArray());
            });
    }

    private static TerminalSubCommandBuilder CreateDepositSubCommand()
    {
        return new TerminalSubCommandBuilder("<deposit_amount>")
            .WithDescription("Deposit as close as possible to input amount")
            .WithMessage("Requesting to deposit value close to [valueFor].\n\nLethal Credit Union will accept the following items for a total of [depositActualTotal]:\n[depositScrapCombo]")
            .EnableConfirmDeny(confirmMessage: "Deposited [numScrapDeposited] scrap items for [depositActualTotal].")
            .WithConditions("d_hasScrapItems", "notEnoughScrap")
            .WithInputMatch(@"^(\d+)$")
            .WithPreAction(input =>
            {
                _valueFor = Convert.ToInt32(input);

                _recommendedScraps = GetScrapForDeposit(_valueFor)
                    .OrderBy(x => x.itemProperties.name)
                    .ThenByDescending(x => x.scrapValue)
                    .ToList();
            })
            .WithAction(() =>
            {
                BankNetworkHandler.Instance.DepositServerRpc(_recommendedScraps.Select(x => x.NetworkObjectId).ToArray());
            });
    }

    private static IEnumerable<GrabbableObject> GetScrapForDeposit(int amount)
    {
        var totalScrapValue = ScrapUtils.GetShipTotalIncludedScrapValue(Plugin.Instance.PluginConfig.BankIgnoreList);
        if (totalScrapValue < amount)
        {
            return new List<GrabbableObject>();
        }

        var nextScrapIndex = 0;
        var allScrap = ScrapUtils.GetAllIncludedScrapInShip(Plugin.Instance.PluginConfig.BankIgnoreList)
            .OrderByDescending(scrap => scrap.itemProperties.twoHanded)
            .ThenByDescending(scrap => scrap.scrapValue)
            .ToList();

        var scrapToSell = new List<GrabbableObject>();

        while (amount > 300) // arbitrary amount until it starts to specifically look for a perfect match, favouring 2handed scrap first
        {
            var nextScrap = allScrap[nextScrapIndex++];
            scrapToSell.Add(nextScrap);
            amount -= nextScrap.scrapValue;
        }

        // Time to actually be precise
        allScrap = allScrap.Skip(nextScrapIndex)
            .OrderBy(scrap => scrap.scrapValue)
            .ToList();
        nextScrapIndex = 0;

        // When trying last few OR a very low amount, just see if it's less than the cheapest item in 'allScrap' list
        if (amount < allScrap.Last().scrapValue)
        {
            scrapToSell.Add(allScrap.Last());
            return scrapToSell;
        }

        while (amount > 0)
        {
            var scrapCombinations = new List<(GrabbableObject First, GrabbableObject Second)>();
            for (var currentIndex = nextScrapIndex; currentIndex < allScrap.Count; currentIndex++)
            {
                for (var nextIndex = currentIndex + 1; nextIndex < allScrap.Count; nextIndex++)
                {
                    scrapCombinations.Add((allScrap[currentIndex], allScrap[nextIndex]));
                }
            }

            var matchingSumForAmountRemaining = scrapCombinations.FirstOrDefault(combo => combo.First.scrapValue + combo.Second.scrapValue >= amount);
            if (matchingSumForAmountRemaining != default)
            {
                scrapToSell.Add(matchingSumForAmountRemaining.First);
                scrapToSell.Add(matchingSumForAmountRemaining.Second);
                return scrapToSell;
            }

            var nextScrap = allScrap[nextScrapIndex++];
            scrapToSell.Add(nextScrap);
            amount -= nextScrap.scrapValue;
        }

        return scrapToSell;
    }
}
