using HarmonyLib;
using LethalCredit.Assets;
using LethalCredit.Manager.Bank;
using LethalCredit.Manager.Saves;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static QualityCompany.Service.GameEvents;

namespace LethalCredit.Network;

internal class ModNetworkManager
{
    private static GameObject _networkPrefab;
    private static bool _hasInit;
    private static readonly List<GameObject> _networkPrefabs = new ();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameNetworkManager), "Start")]
    public static void Start()
    {
        Plugin.Instance.Log.LogMessage("GameNetworkManager.Start");
        SaveManager.Init();
        if (_networkPrefab is not null || _hasInit) return;

        _hasInit = true;

        _networkPrefab = AssetManager.LoadBundleAsset<GameObject>("NetworkHandler");
        _networkPrefab.AddComponent<BankNetworkHandler>();

        // NetworkManager.Singleton.AddNetworkPrefab(_networkPrefab);
        RegisterNetworkPrefab(_networkPrefab);

        foreach (var prefab in _networkPrefabs)
        {
            if (NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(prefab)) return;

            NetworkManager.Singleton.AddNetworkPrefab(prefab);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), "Awake")]
    public static void AwakePatch(StartOfRound __instance)
    {
        Plugin.Instance.Log.LogMessage("StartOfRound.Awake");
        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer) return;
        if (_networkPrefab is null) return;

        var networkHandlerHost = Object.Instantiate(_networkPrefab, Vector3.zero, Quaternion.identity);
        networkHandlerHost.GetComponent<NetworkObject>().Spawn();
    }

    public static void RegisterNetworkPrefab(GameObject prefab)
    {
        if (_networkPrefabs.Contains(prefab)) return;

        _networkPrefabs.Add(prefab);
    }
}
