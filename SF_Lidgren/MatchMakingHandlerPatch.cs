using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SF_Lidgren;

public static class MatchMakingHandlerPatch
{
    public static void Patch(Harmony harmonyInstance)
    {
        var awakeMethod = AccessTools.Method(typeof(MatchmakingHandler), "Awake");
        var awakeMethodPostfix = new HarmonyMethod(typeof(MatchMakingHandlerPatch)
            .GetMethod(nameof(AwakeMethodPostfix)));
        
        harmonyInstance.Patch(awakeMethod, postfix: awakeMethodPostfix);
    }
    
    public static void AwakeMethodPostfix(MatchmakingHandler __instance)
    {
        Debug.Log("Creating join server GUI...");
        __instance.gameObject.AddComponent<TempGUI>();
        
        Debug.Log("Adding MMHSockets...");
        if (!Object.FindObjectOfType<MatchMakingHandlerSockets>())
            __instance.gameObject.AddComponent<MatchMakingHandlerSockets>();
    }
}