using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;
using static QualityCompany.Service.GameEvents;

namespace LethalCredit.Manager.Saves;

internal class SaveManager
{
    private static readonly ModLogger Logger = new(nameof(SaveManager));

    internal static GameSaveData SaveData { get; private set; } = new();

    private static bool IsHost => GameNetworkManager.Instance.isHostingGame;

    private static string _saveFileName;
    private static string _saveFilePath;

    internal static void Init()
    {
        Load();

        Disconnected += _ =>
        {
            SaveData = new();
        };

        SaveGame += _ => Save();

        EndOfGame += instance =>
        {
            Logger.LogMessage($"StartOfRound.EndOfGame, allDead? {instance.allPlayersDead}");

            if (!instance.allPlayersDead) return;

            SaveData.BankBalance = 0;
            Save();
        };

        PlayersFired += _ =>
        {
            SaveData.BankBalance = 0;
            Save();
        };
    }

    internal static void Load()
    {
        if (!IsHost) return;

        var saveNum = GameNetworkManager.Instance.saveFileNum;
        Logger.TryLogDebug($"HOST: using save data file in slot number {saveNum}");
        _saveFileName = $"{PluginMetadata.PLUGIN_NAME}_{saveNum}.json";
        _saveFilePath = Path.Combine(Application.persistentDataPath, _saveFileName);

        if (File.Exists(_saveFilePath))
        {
            Logger.TryLogDebug($"Loading save file: {_saveFileName}");
            var json = File.ReadAllText(_saveFilePath);
            LoadSaveJson(json);
        }
        else
        {
            Logger.TryLogDebug($"No save file found: {_saveFileName}, creating new");
            SaveData = new GameSaveData();
            Save();
        }

        Logger.LogDebug(JsonConvert.SerializeObject(SaveData));
        Plugin.Instance.PluginConfig.DebugPrintConfig(Logger);
    }

    internal static void Save()
    {
        if (!IsHost) return;

        Logger.TryLogDebug($"Saving save data to {_saveFileName}");
        var json = JsonConvert.SerializeObject(SaveData);
        File.WriteAllText(_saveFilePath, json);
    }

    internal static void ClientLoadFromString(string saveJson)
    {
        Logger.TryLogDebug("CLIENT: Save file received from host, updating.");
        LoadSaveJson(saveJson);

        Logger.TryLogDebug(JsonConvert.SerializeObject(SaveData));
        Plugin.Instance.PluginConfig.DebugPrintConfig(Logger);
    }

    private static void LoadSaveJson(string saveJson)
    {
        try
        {
            var jsonSaveData = JsonConvert.DeserializeObject<SaveData>(saveJson);

            SaveData = new GameSaveData
            {
                BankBalance = jsonSaveData.BankBalance
            };
        }
        catch (Exception ex)
        {
            // save file has been edited / corrupted
            Logger.LogError($"Save file has been corrupted or edited, resetting. Error: {ex.Message}");
            SaveData = new GameSaveData();
            Save();
        }
    }
}

[Serializable]
file class SaveData
{
    public int BankBalance { get; set; }
}