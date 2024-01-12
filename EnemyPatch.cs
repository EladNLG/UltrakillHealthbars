using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

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
            GameObject bar = Object.Instantiate(Plugin.healthbar, Plugin.canvas.transform);
            bar.GetComponent<Healthbar>().enemy = __instance;
        }
    }
}
