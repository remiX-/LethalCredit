using HarmonyLib;
using LethalCredit.Assets;
using LethalCredit.Service;
using Unity.Netcode;
using UnityEngine;

namespace LethalCredit.Network;

[HarmonyPatch]
internal class NetworkObjectManager
{
    private static readonly ModLogger _logger = new(nameof(NetworkObjectManager));

    private static GameObject networkPrefab;

    private static bool hasInit;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameNetworkManager), "Start")]
    public static void Init()
    {
        if (networkPrefab != null || hasInit) return;

        hasInit = true;

        networkPrefab = AssetManager.CustomAssets.LoadAsset<GameObject>("ModNetworkHandler");

        NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), "Awake")]
    public static void SpawnNetworkHandlerObject()
    {
        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer) return;

        var networkHandlerHost = Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
        networkHandlerHost.GetComponent<NetworkObject>().Spawn();
    }
}