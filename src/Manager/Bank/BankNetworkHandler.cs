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
            EndOfGame += _ => AutobankScrap();

            return;
        }

        Logger.LogDebug("CLIENT: Requesting hosts config...");
        RequestPluginConfigServerRpc();
        RequestSaveDataServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPluginConfigServerRpc()
    {
        Logger.LogDebug("HOST: A client is requesting plugin config");
        var json = JsonConvert.SerializeObject(Plugin.Instance.PluginConfig);
        SendPluginConfigClientRpc(json);
    }

    [ClientRpc]
    private void SendPluginConfigClientRpc(string json)
    {
        if (IsHost || IsServer) return;

        if (_retrievedPluginConfig)
        {
            Logger.LogDebug("CLIENT: Config has already been received from host, disregarding.");
            return;
        }
        _retrievedPluginConfig = true;

        var cfg = JsonConvert.DeserializeObject<PluginConfig>(json);
        if (cfg is null)
        {
            Logger.LogError($"CLIENT: failed to deserialize plugin config from host, disregarding. raw json: {json}");
            return;
        }

        Logger.LogDebug("Config received, deserializing and constructing...");
        Plugin.Instance.PluginConfig.ApplyHostConfig(cfg);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSaveDataServerRpc()
    {
        Logger.LogDebug("HOST: A client is requesting save data");
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

        HudUtils.DisplayNotification($"Lethal Credit Union balance has increased to ${SaveManager.SaveData.BankBalance}");
    }

    [ServerRpc(RequireOwnership = false)]
    internal void WithdrawServerRpc(int amount, ulong playerClientId)
    {
        var dollarStackItem = (Item)AssetManager.AssetCache["DollarStack"];
        var prefab = dollarStackItem.spawnPrefab;

        var player = GameUtils.StartOfRound.allPlayerScripts.FirstOrDefault(script => script.playerClientId == playerClientId);
        if (player is null)
        {
            Logger.LogFatal($"Tried to execute withdraw with an unknown player script with playerClientId {playerClientId}");
            return;
        }

        var dollarStackScrap = Instantiate(prefab, player.transform.position, Quaternion.identity);
        var itemGrabObj = dollarStackScrap.GetComponent<GrabbableObject>();

        if (itemGrabObj is null)
        {
            Logger.LogFatal($"{dollarStackScrap.name}: did not have a GrabbableObject component");
            return;
        }

        Logger.LogDebug($" > spawned in {dollarStackScrap.name} for {amount}");
        dollarStackScrap.GetComponent<NetworkObject>().Spawn();

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

        HudUtils.DisplayNotification($"{username} has withdrawn ${amount} from LCU Bank!");

        Logger.LogDebug($"Successfully synced values of {prop.itemProperties.itemName}");
    }

    private void AutobankScrap()
    {
        var scrapToBank = ScrapUtils.GetAllIncludedScrapInShip(Plugin.Instance.PluginConfig.BankIgnoreList);
        if (!scrapToBank.Any()) return;

        var totalValue = 0;

        foreach (var go in scrapToBank)
        {
            totalValue += go.scrapValue;
            go.GetComponent<NetworkObject>().Despawn();
        }

        AutobankClientRpc(scrapToBank.Count, totalValue);

        SaveManager.Save();
    }

    [ClientRpc]
    private void AutobankClientRpc(int depositCount, int depositValue)
    {
        Logger.LogDebug("Auto-banking scrap!");
        SaveManager.SaveData.BankBalance += depositValue;

        HudUtils.DisplayNotification($"Lethal Credit Union has auto-banked {depositCount} scrap items. Balance: ${SaveManager.SaveData.BankBalance}");
    }

    #region BETA Testing

    [ClientRpc]
    internal void SyncBankBalanceClientRpc(int amount)
    {
        SaveManager.SaveData.BankBalance = amount;
    }
    #endregion
}