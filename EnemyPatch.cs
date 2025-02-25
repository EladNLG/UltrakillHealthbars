using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using BepInEx;
using System.Runtime.CompilerServices;  

namespace Healthbars.Patches
{
    [HarmonyPatch(typeof(EnemyIdentifier))]
    public class EnemyPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        public static void EnemySpawned(EnemyIdentifier __instance) 
        {
            if (__instance.health < Plugin.minMaxHealth.value)
                return;
            Debug.Log($"healthbar asset: {Plugin.healthbar}\nCanvas: {Plugin.canvas}");
            GameObject bar = Object.Instantiate(Plugin.healthbar, Plugin.canvas.transform);
            bar.GetComponent<Healthbar>().enemy = __instance;
        }
    }
}
