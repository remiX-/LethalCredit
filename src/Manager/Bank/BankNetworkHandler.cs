using LethalCredit.Assets;
using LethalCredit.Manager.Saves;
using Newtonsoft.Json;
using QualityCompany.Utils;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LethalCredit.Manager.Bank;

internal class BankNetworkHandler : NetworkBehaviour
{
    public static BankNetworkHandler Instance { get; private set; }

    private readonly ModLogger _logger = new(nameof(BankNetworkHandler));

    // private bool _retrievedPluginConfig;
    private bool _retrievedSaveFile;

    private void Start()
    {
        Instance = this;

        if (IsHost) return;

        _logger.LogDebug("CLIENT: Requesting hosts config...");
        RequestSaveDataServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSaveDataServerRpc()
    {
        _logger.LogDebug("HOST: A client is requesting save data");
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
    public void DepositClientRpc(int depositValue)
    {
        SaveManager.SaveData.BankBalance += depositValue;

        HudUtils.DisplayNotification($"Lethal Credit Union balance has increased to {SaveManager.SaveData.BankBalance}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void WithdrawServerRpc(int amount)
    {
        var dollarStackItem = (Item)AssetManager.AssetCache["DollarStack"];
        var prefab = dollarStackItem.spawnPrefab;

        var currentPlayerLocation = GameNetworkManager.Instance.localPlayerController.transform.position;
        var dollarStackScrap = Instantiate(prefab, currentPlayerLocation, Quaternion.identity);
        var itemGrabObj = dollarStackScrap.GetComponent<GrabbableObject>();

        if (itemGrabObj is null)
        {
            _logger.LogFatal($"{dollarStackScrap.name}: did not have a GrabbableObject component");
            return;
        }

        _logger.LogDebug($" > spawned in {dollarStackScrap.name} for {amount}");
        dollarStackScrap.GetComponent<NetworkObject>().Spawn();

        SyncValuesClientRpc(amount, new NetworkBehaviourReference(itemGrabObj));
    }

    [ClientRpc]
    public void SyncValuesClientRpc(int amount, NetworkBehaviourReference netRef)
    {
        _logger.LogMessage("SyncValuesClientRpc");
        netRef.TryGet(out GrabbableObject prop);

        if (prop is null)
        {
            _logger.LogFatal("Unable to resolve net ref for SyncValuesClientRpc!");
            return;
        }

        prop.transform.parent = GameUtils.ShipGameObject.transform;

        if (amount == 0) return;

        prop.scrapValue = amount;
        prop.itemProperties.creditsWorth = amount;
        prop.GetComponentInChildren<ScanNodeProperties>().subText = $"Value: ${amount}";

        SaveManager.SaveData.BankBalance -= amount;

        _logger.LogInfo($"Successfully synced values of {prop.itemProperties.itemName}");
    }
}
