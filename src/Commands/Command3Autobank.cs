﻿using LethalCredit.Manager.Bank;
using QualityCompany.Manager.ShipTerminal;

namespace LethalCredit.Commands;

internal class Command3Autobank
{
    [TerminalCommand]
    private static TerminalCommandBuilder Autobank()
    {
        AdvancedTerminal.AddGlobalTextReplacement("[lcu__autobankStatus]", () => Plugin.Instance.PluginConfig.AutoBankAtEndOfRound ? "on" : "off");

        return new TerminalCommandBuilder("lcu-autobank")
            .WithHelpDescription("Status: [lcu__autobankStatus]\nTurn LCU autobank feature on / off. Autobank will automatically deposit all matching scrap at the end of a game round.\nNote:This does not update the config")
            .WithAction(() =>
            {
                var newStatus = !Plugin.Instance.PluginConfig.AutoBankAtEndOfRound;

                BankNetworkHandler.Instance.ToggleAutobankStatusServerRpc();

                if (newStatus)
                {
                    return "Autobank has been enabled!\n\nAll matching scrap at the end of game rounds will be automatically banked into LCU Bank.";
                }

                return "Autobank has been disabled!";
            });
    }
}
