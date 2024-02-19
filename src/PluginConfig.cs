using BepInEx.Configuration;
using Newtonsoft.Json;
using System;

namespace LethalCredit;

[Serializable]
internal class PluginConfig
{
    #region Debug
    [JsonIgnore]
    public bool ShowDebugLogs { get; set; }
    #endregion

    public PluginConfig()
    { }

    public void Bind(ConfigFile configFile)
    {
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
    }
}
