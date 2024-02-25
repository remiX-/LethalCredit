using LethalCredit.Assets;
using LethalCredit.Manager.Bank;
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

    internal static void Init()
    {
        GameNetworkManagerStart += _ => Start();
        StartOfRoundAwake += _ => Load();
    }

    public static void Start()
    {
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

    public static void Load()
    {
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
