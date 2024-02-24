using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LethalCredit.Assets;

internal class AssetManager
{
    private static readonly ModLogger Logger = new(nameof(AssetManager));

    internal static AssetBundle CustomAssets;

    private static string _modRoot;
    private static readonly Dictionary<string, string> AssetPaths = new()
    {
        { "ATM", "Assets/LethalCredit/Prefabs/ATM.prefab" },
        { "CreditCard", "Assets/LethalCredit/Prefabs/CreditCardItem.asset" },
        { "DollarStack", "Assets/LethalCredit/Prefabs/DollarStackItem.asset" }
    };
    internal static readonly Dictionary<string, GameObject> Prefabs = new();

    public static readonly Dictionary<string, Object> AssetCache = new();

    internal static void LoadModBundle(string root)
    {
        _modRoot = root;
        CustomAssets = AssetBundle.LoadFromFile(Path.Combine(_modRoot, "lethalcreditbundle"));
        if (CustomAssets is null)
        {
            Logger.LogError("Failed to load custom assets!");
        }
    }

    public static T LoadBundleAsset<T>(string itemName) where T : Object
    {
        if (AssetCache.TryGetValue(itemName, out var value)) return (T)value;

        if (AssetPaths.TryGetValue(itemName, out var path))
        {
            var asset = TryLoadBundleAsset<T>(ref CustomAssets, path);
            AssetCache.Add(itemName, asset);
            return asset;
        }

        Logger.LogError($"{itemName} was not present in the asset or sample dictionary!");
        return default;
    }

    internal static T TryLoadBundleAsset<T>(ref AssetBundle bundle, string path) where T : Object
    {
        var result = bundle.LoadAsset<T>(path);

        if (result is null)
        {
            Logger.LogError($"An error has occurred trying to load asset from {path}");
            return null;
        }

        Logger.LogDebug($"Loaded asset located in {path}");
        return result;
    }

    internal static void AddPrefab(string name, GameObject prefab)
    {
        Prefabs.Add(name, prefab);
    }
}
