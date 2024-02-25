using BepInEx.Configuration;
using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace LethalCredit;

[Serializable]
internal class PluginConfig
{
    #region Bank
    public string BankIgnoreList { get; set; }

    public bool AllowBankingCredits { get; set; }

    public int BankCreditsRatePercentage { get; set; }
    #endregion

    #region Debug
    [JsonIgnore]
    public bool ShowDebugLogs { get; set; }
    #endregion

    public PluginConfig()
    { }

    public void Bind(ConfigFile configFile)
    {
        #region Terminal
        BankIgnoreList = configFile.Bind(
            "Bank",
            "Bank Ignore list",
            "shotgun,gunammo,gift",
            "[HOST] A comma separated list of items to ignore in the ship when depositing items to Lethal Credit Union. Does not have to be the exact name but at least a matching portion. e.g. 'trag' for 'tragedy'"
        ).Value;

        AllowBankingCredits = configFile.Bind(
            "Bank",
            "Allow banking credits",
            true,
            "[HOST] Whether the bank will accept you offering up available credits. This is only allowed on the day of deadline."
        ).Value;

        BankCreditsRatePercentage = configFile.Bind(
            "Bank",
            "Banking credits rate percentage",
            50,
            "[HOST] The banking percentage rate of giving your credits to The Lethal Credit Union."
        ).Value;
        #endregion

        #region Debug
        ShowDebugLogs = configFile.Bind(
            "Debug",
            "ShowDebugLogs",
            false,
            "[CLIENT] Turn on/off debug logs."
        ).Value;
        #endregion
    }

    public void ApplyHostConfig(PluginConfig hostConfig)
    {
        BankIgnoreList = hostConfig.BankIgnoreList;
        AllowBankingCredits = hostConfig.AllowBankingCredits;
        BankCreditsRatePercentage = hostConfig.BankCreditsRatePercentage;
    }

    public void DebugPrintConfig(ModLogger logger)
    {
        foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(this))
        {
            var name = descriptor.Name;
            var value = descriptor.GetValue(this);
            logger.LogDebug($"{name}={value}");
        }
    }
}
