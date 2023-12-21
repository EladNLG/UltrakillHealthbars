using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Healthbars
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance;
        public static GameObject healthbar;
        public static Canvas canvas;
        private void Awake()
        {
            instance = this;
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            AssetBundle bundle = AssetUtils.LoadAssetBundleFromResources("healthbars", System.Reflection.Assembly.GetExecutingAssembly());
            if (bundle == null)
                throw new System.Exception("tHE FUCK 3");
            healthbar = bundle.LoadAsset<GameObject>("Healthbar");
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loade2d!");
            if (healthbar == null)
                throw new System.Exception("tHE FUCK");
            canvas = Instantiate(bundle.LoadAsset<GameObject>("HealthbarCanvas")).GetComponent<Canvas>();
            DontDestroyOnLoad(canvas);
            if (canvas == null)
                throw new System.Exception("tHE FUCK 2");
            Logger.LogInfo($"Canvas Ready: {canvas != null}");
            Harmony patcher = new Harmony("me.eladnlg.healthbars");
            patcher.PatchAll();

        }
    }
}
