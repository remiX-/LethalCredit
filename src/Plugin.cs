using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalCredit.Assets;
using LethalLib.Extras;
using LethalLib.Modules;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LethalCredit;

[BepInPlugin(PluginMetadata.PLUGIN_GUID, PluginMetadata.PLUGIN_NAME, PluginMetadata.PLUGIN_VERSION)]
[BepInDependency("evaisa.lethallib")]
[BepInDependency("um_no.QualityCompany")]
public class Plugin : BaseUnityPlugin
{
    private readonly Harmony harmony = new(PluginMetadata.PLUGIN_GUID);

    internal static Plugin Instance;

    internal ManualLogSource Log;

    internal PluginConfig PluginConfig;

    internal string PluginPath;

    private void Awake()
    {
        Instance = this;
        Log = BepInEx.Logging.Logger.CreateLogSource(PluginMetadata.PLUGIN_NAME);

        // Asset Bundles
        PluginPath = Path.GetDirectoryName(Info.Location)!;

        // Config
        PluginConfig = new PluginConfig();
        PluginConfig.Bind(Config);

        // Plugin patch logic
        NetcodePatcher();
        Patch();
        LoadAssets();

        // Loaded
        Log.LogMessage($"Plugin {PluginMetadata.PLUGIN_NAME} v{PluginMetadata.PLUGIN_VERSION} is loaded!");
    }

    private void Patch()
    {
        // AdvancedTerminalRegistry.Register(Assembly.GetExecutingAssembly(), description: "QualityCompany provides auto-sell functionality with a few commands.");
        // ModuleRegistry.Register(Assembly.GetExecutingAssembly());

        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    private static void NetcodePatcher()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length > 0)
                {
                    method.Invoke(null, null);
                }
            }
        }
    }

    private void LoadAssets()
    {
        AssetManager.LoadModBundle(PluginPath);

        var atmItem = AssetManager.GetItemObject("ATM");

        NetworkPrefabs.RegisterNetworkPrefab(atmItem);
        AssetManager.AddPrefab("ATM", atmItem);

        var itemInfoNode = ScriptableObject.CreateInstance<TerminalNode>();
        itemInfoNode.clearPreviousText = true;
        itemInfoNode.name = "atm_itemInfo";
        itemInfoNode.displayText = "atm_test";

        Unlockables.RegisterUnlockable(
            new UnlockableItemDef
            {
                storeType = StoreType.ShipUpgrade,
                unlockable = new UnlockableItem
                {
                    unlockableName = "atm",
                    spawnPrefab = true,
                    prefabObject = AssetManager.Prefabs["ATM"],
                    IsPlaceable = true,
                    alwaysInStock = true,
                    unlockableType = 1
                }
            }, StoreType.ShipUpgrade,
            itemInfo: itemInfoNode,
            price: 1
        );
    }
}