using Newtonsoft.Json;
using System;
using static QualityCompany.Events.GameEvents;

namespace LethalCredit.Manager.Saves;

internal class SaveManager
{
    private static readonly ModLogger Logger = new(nameof(SaveManager));

    internal static GameSaveData SaveData { get; private set; } = new();

    private static bool IsHost => GameNetworkManager.Instance.isHostingGame;

    private const string SaveDataKey = "UMNO_LCU_DATA";

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

        var saveFile = GameNetworkManager.Instance.currentSaveFileName;
        Logger.LogDebug($"HOST: using game save file at {saveFile}");

        var json = (string)ES3.Load(key: SaveDataKey, defaultValue: null, filePath: saveFile);
        if (json is not null)
        {
            Logger.LogDebug(" > data found! loading...");
            LoadSaveJson(json);
        }
        else
        {
            Logger.LogDebug(" > no data! starting new...");
            Save();
        }
    }

    internal static void Save()
    {
        if (!IsHost) return;

        var saveFile = GameNetworkManager.Instance.currentSaveFileName;
        var json = JsonConvert.SerializeObject(SaveData);
        ES3.Save(key: SaveDataKey, value: json, filePath: saveFile);
    }

    internal static void ClientLoadFromString(string saveJson)
    {
        Logger.TryLogDebug("CLIENT: Save file received from host, updating.");
        LoadSaveJson(saveJson);
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

            Logger.TryLogDebug(JsonConvert.SerializeObject(SaveData));
            Plugin.Instance.PluginConfig.DebugPrintConfig(Logger);
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