using GameNetcodeStuff;
using LethalCredit.Assets;
using LethalCredit.Manager.Saves;
using Newtonsoft.Json;
using QualityCompany.Utils;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static QualityCompany.Service.GameEvents;

namespace LethalCredit.Manager.Bank;

internal class BankNetworkHandler : NetworkBehaviour
{
    public static BankNetworkHandler Instance { get; private set; }

    private readonly ModLogger Logger = new(nameof(BankNetworkHandler));

    private bool _retrievedPluginConfig;
    private bool _retrievedSaveFile;

    private void Start()
    {
        Instance = this;

        if (IsHost)
        {
            SaveGame += AutobankScrap;

            return;
        }

        Logger.TryLogDebug("CLIENT: Requesting hosts config...");
        RequestPluginConfigServerRpc();
        RequestSaveDataServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPluginConfigServerRpc()
    {
        Logger.TryLogDebug("HOST: A client is requesting plugin config");
        var json = JsonConvert.SerializeObject(Plugin.Instance.PluginConfig);
        SendPluginConfigClientRpc(json);
    }

    [ClientRpc]
    private void SendPluginConfigClientRpc(string json)
    {
        if (IsHost || IsServer) return;

        if (_retrievedPluginConfig)
        {
            Logger.TryLogDebug("CLIENT: Config has already been received from host, disregarding.");
            return;
        }
        _retrievedPluginConfig = true;

        var cfg = JsonConvert.DeserializeObject<PluginConfig>(json);
        if (cfg is null)
        {
            Logger.LogError($"CLIENT: failed to deserialize plugin config from host, disregarding. raw json: {json}");
            return;
        }

        Logger.TryLogDebug("Config received, deserializing and constructing...");
        Plugin.Instance.PluginConfig.ApplyHostConfig(cfg);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSaveDataServerRpc()
    {
        Logger.TryLogDebug("HOST: A client is requesting save data");
        var json = JsonConvert.SerializeObject(SaveManager.SaveData);
        SendSaveDataClientRpc(json);
    }

    [ClientRpc]
    private void SendSaveDataClientRpc(string json)
    {
        if (IsHost || IsServer) return;
        if (_retrievedSaveFile) return;
        _retrievedSaveFile = true;

        SaveManager.ClientLoadFromString(json);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DepositServerRpc(ulong[] networkObjectId)
    {
        var scraps = ScrapUtils
            .GetAllScrapInShip()
            .Where(x => networkObjectId.Contains(x.NetworkObjectId));

        var totalValue = 0;

        foreach (var go in scraps)
        {
            totalValue += go.scrapValue;
            go.GetComponent<NetworkObject>().Despawn();
        }

        DepositClientRpc(totalValue);
    }

    [ClientRpc]
    private void DepositClientRpc(int depositValue)
    {
        SaveManager.SaveData.BankBalance += depositValue;

        HudUtils.DisplayNotification($"LCU balance has increased to ${SaveManager.SaveData.BankBalance}");
    }

    [ServerRpc(RequireOwnership = false)]
    internal void WithdrawServerRpc(int amount, ulong playerClientId)
    {
        var lcuBucks = (Item)AssetManager.AssetCache["LCUBucks"];
        var lcuBucksSpawnPrefab = lcuBucks.spawnPrefab;

        var player = GameUtils.StartOfRound.allPlayerScripts.FirstOrDefault(script => script.playerClientId == playerClientId);
        if (player is null)
        {
            Logger.LogFatal($"Tried to execute withdraw with an unknown player script with playerClientId {playerClientId}");
            return;
        }

        var lcuBucksGameObject = Instantiate(lcuBucksSpawnPrefab, player.transform.position, Quaternion.identity);
        var itemGrabObj = lcuBucksGameObject.GetComponent<GrabbableObject>();

        if (itemGrabObj is null)
        {
            Logger.LogFatal($"{lcuBucksGameObject.name}: did not have a GrabbableObject component");
            return;
        }

        Logger.TryLogDebug($" > spawned in {lcuBucksGameObject.name} for {amount}");
        lcuBucksGameObject.GetComponent<NetworkObject>().Spawn();

        WithdrawClientRpc(amount, player.playerUsername, new NetworkBehaviourReference(itemGrabObj));
    }

    [ClientRpc]
    private void WithdrawClientRpc(int amount, string username, NetworkBehaviourReference netRef)
    {
        netRef.TryGet(out GrabbableObject prop);

        if (prop is null)
        {
            Logger.LogFatal("Unable to resolve net ref for WithdrawClientRpc!");
            return;
        }

        prop.transform.parent = GameUtils.ShipGameObject.transform;

        if (amount == 0) return;

        prop.scrapValue = amount;
        prop.itemProperties.creditsWorth = amount;
        prop.GetComponentInChildren<ScanNodeProperties>().subText = $"Value: ${amount}";

        SaveManager.SaveData.BankBalance -= amount;

        HudUtils.DisplayNotification($"LCU: {username} has withdrawn ${amount}! Balance: ${SaveManager.SaveData.BankBalance}");

        Logger.TryLogDebug($"Successfully synced values of {prop.itemProperties.itemName}");
    }

    private void AutobankScrap(GameNetworkManager instance)
    {
        if (!Plugin.Instance.PluginConfig.AutoBankAtEndOfRound)
        {
            Logger.LogDebug("Autobank is disabled, skipping");
            return;
        }

        Logger.TryLogDebug($"On moon: {GameUtils.CurrentPlanet()} | {GameUtils.CurrentLevel()} | {GameUtils.IsOnCompany()} | isDC? {instance.isDisconnecting}");
        if (instance.isDisconnecting) return;

        if (GameUtils.IsOnCompany())
        {
            Logger.TryLogDebug("On the company, will not autobank");
            return;
        }

        var scrapToBank = ScrapUtils.GetAllIncludedScrapInShip(Plugin.Instance.PluginConfig.BankIgnoreList);
        if (!scrapToBank.Any())
        {
            Logger.LogDebug("No items to autobank on round ended.");
            return;
        }

        var totalValue = 0;
        Logger.LogDebug("Autobank starting...");

        foreach (var go in scrapToBank)
        {
            Logger.LogDebug($" > {go.name} for {go.scrapValue}");
            var networkObject = go.GetComponent<NetworkObject>();
            if (networkObject is null)
            {
                Logger.LogError("  > is null, ignoring");
                continue;
            }
            if (!networkObject.IsSpawned)
            {
                Logger.LogError("  > is NOT spawned?? ignoring");
                continue;
            }

            networkObject.Despawn();

            totalValue += go.scrapValue;
        }
        Logger.LogDebug($"Autobank complete! Banked {scrapToBank.Count} scrap for a total of {totalValue}");

        AutobankClientRpc(scrapToBank.Count, totalValue);

        SaveManager.Save();
    }

    [ClientRpc]
    private void AutobankClientRpc(int depositCount, int depositValue)
    {
        var oldBalance = SaveManager.SaveData.BankBalance;
        SaveManager.SaveData.BankBalance += depositValue;
        Logger.TryLogDebug($"Autobank: balance increased from {oldBalance} to {SaveManager.SaveData.BankBalance}");

        HudUtils.DisplayNotification($"LCU has auto-banked {depositCount} scrap items. Balance: ${SaveManager.SaveData.BankBalance}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void BankCreditsServerRpc(int amount)
    {
        BankCreditsClientRpc(amount, BankUtils.GetBankAmountForCredits(amount));
    }

    [ClientRpc]
    private void BankCreditsClientRpc(int amount, int bankedAmount)
    {
        Logger.LogDebug($"Banking {amount} credits for {bankedAmount} LCU bucks");
        SaveManager.SaveData.BankBalance += bankedAmount;
        GameUtils.Terminal.groupCredits -= amount;

        HudUtils.DisplayNotification($"LCU has banked ${amount} credits. Balance: ${SaveManager.SaveData.BankBalance}");
    }

    [ServerRpc(RequireOwnership = false)]
    internal void ToggleAutobankStatusServerRpc()
    {
        ToggleAutobankStatusClientRpc();
    }

    [ClientRpc]
    private void ToggleAutobankStatusClientRpc()
    {
        Plugin.Instance.PluginConfig.AutoBankAtEndOfRound = !Plugin.Instance.PluginConfig.AutoBankAtEndOfRound;

        HudUtils.DisplayNotification($"Autobank has been {(Plugin.Instance.PluginConfig.AutoBankAtEndOfRound ? "enabled" : "disabled")}");
    }

    #region BETA Testing

    [ClientRpc]
    internal void SyncBankBalanceClientRpc(int amount)
    {
        SaveManager.SaveData.BankBalance = amount;

        HudUtils.DisplayNotification($"LCU: The host has forced bank balance to ${amount}");
    }
    #endregion
}