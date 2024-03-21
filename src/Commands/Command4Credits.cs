using LethalCredit.Manager.Bank;
using LethalCredit.Manager.Saves;
using QualityCompany.Manager.ShipTerminal;
using QualityCompany.Utils;
using System;

namespace LethalCredit.Commands;

internal class Command4Credits
{
    private static int _creditsFor;
    private static int _bankValueFor;
    private static int _newCredits;
    private static int _newBalance;

    [TerminalCommand]
    private static TerminalCommandBuilder Credits()
    {
        if (!Plugin.Instance.PluginConfig.AllowBankingCredits) return null;

        return new TerminalCommandBuilder("lcu-credits")
            .WithHelpDescription("Bank all or some of your credits to The Lethal Credit Union Bank")
            .WithSubCommand(CreateCreditsAllCommand())
            .WithSubCommand(CreateCreditsAmountCommand())
            .AddTextReplacement("[creditsFor]", () => $"${_creditsFor}")
            .AddTextReplacement("[bankValueFor]", () => $"${_bankValueFor}")
            .AddTextReplacement("[newCredits]", () => $"${_newCredits}")
            .AddTextReplacement("[newBalance]", () => $"${_newBalance}")
            .AddTextReplacement("[currentCredits]", () => $"${GameUtils.Terminal.groupCredits}")
            .AddTextReplacement("[bankRate]", () => $"{Plugin.Instance.PluginConfig.BankCreditsRatePercentage}%")
            .WithCondition("notEnoughCredits", "You do not have enough credits.\n\nCurrent credits: [currentCredits].", () => GameUtils.Terminal.groupCredits >= _creditsFor);
    }

    private static TerminalSubCommandBuilder CreateCreditsAllCommand()
    {
        return new TerminalSubCommandBuilder("all")
            .WithDescription("Withdraw current quota")
            .WithMessage("Requesting to trade [creditsFor] credits at a rate of [bankRate].\n\n" +
                         "Current credits: [currentCredits]\n" +
                         "Trade value: [bankValueFor] LCU bucks\n\n" +
                         "Forecasted credits balance: [newCredits]\n" +
                         "Forecasted LCU bank balance: [newBalance] LCU bucks")
            .EnableConfirmDeny(confirmMessage: "Banked [creditsFor] credits for [bankValueFor] LCU bucks.\n\nCredits balance: [newCredits]\nYour balance: [newBalance]")
            .WithConditions("notEnoughCredits")
            .WithPreAction(() =>
            {
                _creditsFor = GameUtils.Terminal.groupCredits;
                _bankValueFor = BankUtils.GetBankAmountForCredits(_creditsFor);
                _newCredits = 0;
                _newBalance = SaveManager.SaveData.BankBalance + _bankValueFor;
            })
            .WithAction(() =>
            {
                BankNetworkHandler.Instance.BankCreditsServerRpc(_creditsFor);
            });
    }

    private static TerminalSubCommandBuilder CreateCreditsAmountCommand()
    {
        return new TerminalSubCommandBuilder("<amount>")
            .WithDescription("Bank a specific amount")
            .WithMessage("Requesting to trade [creditsFor] credits at a rate of [bankRate].\n\n" +
                         "Current credits: [currentCredits]\n" +
                         "Trade value: [bankValueFor] LCU bucks\n\n" +
                         "Forecasted credits balance: [newCredits]\n" +
                         "Forecasted LCU bank balance: [newBalance] LCU bucks")
            .EnableConfirmDeny(confirmMessage: "Banked [creditsFor] credits for [bankValueFor] LCU bucks.\n\nCredits balance: [newCredits]\nYour balance: [newBalance]")
            .WithConditions("notEnoughCredits")
            .WithInputMatch(@"^(\d+)$")
            .WithPreAction(input =>
            {
                _creditsFor = Convert.ToInt32(input);
                _bankValueFor = BankUtils.GetBankAmountForCredits(_creditsFor);
                _newCredits = GameUtils.Terminal.groupCredits - _creditsFor;
                _newBalance = SaveManager.SaveData.BankBalance + _bankValueFor;
            })
            .WithAction(() =>
            {
                BankNetworkHandler.Instance.BankCreditsServerRpc(_creditsFor);
            });
    }
}
