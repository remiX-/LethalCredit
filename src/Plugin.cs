using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalCredit.Assets;
using LethalCredit.Network;
using LethalLib.Extras;
using LethalLib.Modules;
using QualityCompany.Manager.ShipTerminal;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LethalCredit;

[BepInPlugin(PluginMetadata.PLUGIN_GUID, PluginMetadata.PLUGIN_NAME, PluginMetadata.PLUGIN_VERSION)]
[BepInDependency("umno.QualityCompany", "1.4.0")]
#if DEBUG
[BepInDependency("evaisa.lethallib")]
#endif
public class Plugin : BaseUnityPlugin
{
    internal static Plugin Instance;
    internal ManualLogSource Log;
    internal PluginConfig PluginConfig;
    internal string PluginPath;

    private readonly Harmony _harmony = new(PluginMetadata.PLUGIN_GUID);

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

    private void Patch()
    {
        AdvancedTerminalRegistry.Register(Assembly.GetExecutingAssembly(), commandKeyword: "lcu", description: "Lethal Credit Union Bank, here to bank your scrap.");

        _harmony.PatchAll(typeof(ModNetworkManager));
    }

    private void LoadAssets()
    {
        AssetManager.LoadModBundle(PluginPath);

        var ds = AssetManager.LoadBundleAsset<Item>("DollarStack");

#if RELEASE
        ModNetworkManager.RegisterNetworkPrefab(ds.spawnPrefab);
#elif DEBUG
        var atmItem = AssetManager.LoadBundleAsset<GameObject>("ATM");

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

        // cc
        var cc = AssetManager.LoadBundleAsset<Item>("CreditCard");
        Utilities.FixMixerGroups(cc.spawnPrefab);
        NetworkPrefabs.RegisterNetworkPrefab(cc.spawnPrefab);

        var infoNode = ScriptableObject.CreateInstance<TerminalNode>();
        infoNode.clearPreviousText = true;
        infoNode.displayText = "A credit card?!\n\n";
        Items.RegisterShopItem(cc, 25);

        // dollarstack
        Utilities.FixMixerGroups(ds.spawnPrefab);
        NetworkPrefabs.RegisterNetworkPrefab(ds.spawnPrefab);

        var dsinfoNode = ScriptableObject.CreateInstance<TerminalNode>();
        dsinfoNode.clearPreviousText = true;
        dsinfoNode.displayText = "Money.\n\n";
        Items.RegisterShopItem(ds, 25);
#endif
    }
}