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
        public const bool ENABLE_STAMINA_BAR = false;
        public const bool PER_PHASE_SEGMENTING = false;
        public static Plugin instance;
        public static GameObject ob;
        public static GameObject healthbar;
        public static Canvas canvas;
        
        // configuration stuff
        internal static PluginConfigurator config;
        internal static EnumField<DisplayMode> displayMode;

        public static ColorField bossColor;
        public static ColorField normalColor;
        public static ColorField blessedColor;
        public static FloatField width;
        public static FloatField widthExponent;
        public static IntField height;
#if ENABLE_STAMINA_BAR
        public static ColorField staminaColor;
#endif
        public static FloatField minMaxHealth;
        public static BoolField useTotalHealth;
        public static EnumField<SegmentMode> segmentedBars;
        public static BoolField showHealthLabel;
        public static BoolField showMaxHealth;


        private void Awake()
        {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            Harmony patcher = new Harmony("me.eladnlg.healthbars");
            patcher.PatchAll();

            ob = gameObject;
            instance = this;
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            // load included bundle
            AssetBundle bundle = AssetUtils.LoadAssetBundleFromResources("healthbars", System.Reflection.Assembly.GetExecutingAssembly());

            // healthbar prefab
            healthbar = bundle.LoadAsset<GameObject>("Healthbar");
            if (healthbar == null)
                throw new System.NullReferenceException("Healthbar asset is null!");
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loade2d!");

            // canvas prefab
            canvas = Instantiate(bundle.LoadAsset<GameObject>("HealthbarCanvas")).GetComponent<Canvas>();
            DontDestroyOnLoad(canvas.gameObject);
            canvas.gameObject.hideFlags = HideFlags.HideAndDontSave; // scene cleanup do be aggressive tho
            
            // cba to reinstantiate every scene change tbh

            // PluginConfigurator
            config = PluginConfigurator.Create("Healthbars", "me.eladnlg.healthbars");
            config.SetIconWithURL("https://raw.githubusercontent.com/EladNLG/UltrakillHealthbars/main/assets/icon.png");
            displayMode = new EnumField<DisplayMode>(config.rootPanel, "Display Mode", "displayMode", DisplayMode.Accurate);
            displayMode.SetEnumDisplayName(DisplayMode.Accurate, "Accurate/Vanilla");

            width = new FloatField(config.rootPanel, "Width Multiplier", "width", 1f);
            width.maximumValue = 2;
            width.minimumValue = 0.05f;
            new ConfigHeader(config.rootPanel,  
                @"Changes the width for higher-health enemies. 
0 means the healthbar wont change its size at all, 
1 means it scales linearly with enemy health.", 16);
            widthExponent = new FloatField(config.rootPanel, "Width Exponent", "widthExponent", 0.5f);
            widthExponent.maximumValue = 1f;
            widthExponent.minimumValue = 0f;
            height = new IntField(config.rootPanel, "Height", "height", 3);
            height.minimumValue = 1;
            height.maximumValue = 8;
            // COLORS //
            ConfigPanel colorPanel = new ConfigPanel(config.rootPanel, "Colors", "colors");

            bossColor = new ColorField(colorPanel, "Boss Healthbar Color", "bossColor", new Color(1f, 0.28f, 0.28f));
            normalColor = new ColorField(colorPanel, "Regular Healthbar Color", "normalColor", new Color(1f, 0.82f, 0.28f));
            blessedColor = new ColorField(colorPanel, "Blessed Healthbar Color", "blessedColor", new Color(0.28f, 0.82f, 1f));
#if ENABLE_STAMINA_BAR
            staminaColor = new ColorField(colorPanel, "Enemy Stamina Color", "staminaColor", new Color(0.28f, 0.82f, 1f));
#endif

            new ConfigHeader(config.rootPanel, 
                @"The minimum BASE health (inclusive) of an enemy to display a healthbar.
Personal Recommendations:
5 - Schisms and above display healthbar
4.5 - Streetcleaners and above display healthbar
0 - All enemies display healthbar", 16);
            minMaxHealth = new FloatField(config.rootPanel, "Min Health", "minHealth", 0f);
            segmentedBars = new EnumField<SegmentMode>(config.rootPanel, "Segmented Bars", "segmentedBars", SegmentMode.Per5Health);
            useTotalHealth = new BoolField(config.rootPanel, "Increase healthbar length with radiance tiers", "useTotalHealth", true);
            showHealthLabel = new BoolField(config.rootPanel, "Show Health Text", "showHealthLabel", true);
            showMaxHealth = new BoolField(config.rootPanel, "Show Max Health Text", "showMaxHealth", true);

#if ENABLE_STAMINA_BAR
            new ConfigHeader(config.rootPanel, "Enemy Stamina");
            new ConfigHeader(config.rootPanel, @"""stamina"", in this sense, refers to a custom gauge below the enemy healthbar to display a certain value.
These can be disabled completely below, or enabled per-enemy ", 16);
#endif

        }
    }
}
