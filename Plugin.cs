using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using PluginConfig.API;
using PluginConfig.API.Fields;
using PluginConfig.API.Decorators;
using JetBrains.Annotations;
using PluginConfiguratorComponents;

namespace Healthbars
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance;
        public static GameObject healthbar;
        public static Canvas canvas;
        
        // configuration stuff
        internal static PluginConfigurator config;
        internal static EnumField<DisplayMode> displayMode;

        public static ColorField bossColor;
        public static ColorField normalColor;
        public static ColorField blessedColor;
        public static FloatField minMaxHealth;
        public static BoolField useTotalHealth;

        private void Awake()
        {
            instance = this;
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            // load included bundle
            AssetBundle bundle = AssetUtils.LoadAssetBundleFromResources("healthbars", System.Reflection.Assembly.GetExecutingAssembly());

            // healthbar prefab
            healthbar = bundle.LoadAsset<GameObject>("Healthbar");
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loade2d!");

            // canvas prefab
            canvas = Instantiate(bundle.LoadAsset<GameObject>("HealthbarCanvas")).GetComponent<Canvas>();
            // cba to reinstantiate every scene change tbh
            DontDestroyOnLoad(canvas);

            // PluginConfigurator
            config = PluginConfigurator.Create("Healthbars", "me.eladnlg.healthbars");
            config.SetIconWithURL("https://raw.githubusercontent.com/EladNLG/UltrakillHealthbars/main/assets/icon.png");
            displayMode = new EnumField<DisplayMode>(config.rootPanel, "Display Mode", "displayMode", DisplayMode.Normalized);
            bossColor = new ColorField(config.rootPanel, "Boss Healthbar Color", "bossColor", new Color(1f, 0.28f, 0.28f));
            normalColor = new ColorField(config.rootPanel, "Regular Healthbar Color", "normalColor", new Color(1f, 0.82f, 0.28f));
            blessedColor = new ColorField(config.rootPanel, "Blessed Healthbar Color", "blessedColor", new Color(0.28f, 0.82f, 1f));
            new ConfigHeader(config.rootPanel, 
                @"The minimum BASE health (inclusive) of an enemy to display a healthbar.
Personal Recommendations:
5 - Schisms and above display healthbar
4.5 - Streetcleaners and above display healthbar
0 - All enemies display healthbar", 16);
            minMaxHealth = new FloatField(config.rootPanel, "Min Health", "minHealth", 0f);
            useTotalHealth = new BoolField(config.rootPanel, "Increase healthbar length with radiance tiers", "useTotalHealth", false);
            
            Harmony patcher = new Harmony("me.eladnlg.healthbars");
            patcher.PatchAll();

        }
    }
}
