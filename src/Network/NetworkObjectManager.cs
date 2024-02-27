// using HarmonyLib;
// using LethalCredit.Assets;
// using LethalCredit.Manager.Bank;
// using Unity.Netcode;
// using UnityEngine;
//
// namespace LethalCredit.Network;
//
// [HarmonyPatch]
// internal class NetworkObjectManager
// {
//     private static readonly ModLogger Logger = new(nameof(NetworkObjectManager));
//
//     private static GameObject networkPrefab;
//
//     private static bool hasInit;
//
//     [HarmonyPostfix]
//     [HarmonyPatch(typeof(GameNetworkManager), "Start")]
//     public static void Init()
//     {
//         if (networkPrefab != null || hasInit) return;
//
//         GameNetworkManagerStart 
//         hasInit = true;
//
//         networkPrefab = AssetManager.CustomAssets.LoadAsset<GameObject>("ModNetworkHandler");
//         networkPrefab.AddComponent<BankNetworkHandler>();
//         NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
//     }
//
//     [HarmonyPostfix]
//     [HarmonyPatch(typeof(StartOfRound), "Awake")]
//     public static void SpawnNetworkHandlerObject()
//     {
//         if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer) return;
//
//         var networkHandlerHost = Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
//         networkHandlerHost.GetComponent<NetworkObject>().Spawn();
//     }
// }