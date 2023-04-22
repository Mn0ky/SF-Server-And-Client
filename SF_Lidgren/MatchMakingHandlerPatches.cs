using HarmonyLib;
using Lidgren.Network;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SF_Lidgren;

public static class MatchMakingHandlerPatches
{
    public static void Patches(Harmony harmonyInstance)
    {
        var awakeMethod = AccessTools.Method(typeof(MatchmakingHandler), "Awake");
        var awakeMethodPostfix = new HarmonyMethod(typeof(MatchMakingHandlerPatches)
            .GetMethod(nameof(AwakeMethodPostfix)));
        
        var getIsInsideLobbyMethod = AccessTools.Method(typeof(MatchmakingHandler), "get_IsInsideLobby");
        var getIsInsideLobbyMethodPrefix = new HarmonyMethod(typeof(MatchMakingHandlerPatches)
            .GetMethod(nameof(GetIsInsideLobbyMethodPrefix)));
        
        harmonyInstance.Patch(awakeMethod, postfix: awakeMethodPostfix);
        harmonyInstance.Patch(getIsInsideLobbyMethod, prefix: getIsInsideLobbyMethodPrefix);
    }
    
    public static void AwakeMethodPostfix(MatchmakingHandler __instance)
    {
        Debug.Log("Creating join server GUI...");
        __instance.gameObject.AddComponent<TempGUI>();
        
        Debug.Log("Adding MMHSockets...");
        if (!Object.FindObjectOfType<MatchMakingHandlerSockets>())
            __instance.gameObject.AddComponent<MatchMakingHandlerSockets>();
    }

    public static bool GetIsInsideLobbyMethodPrefix(ref bool __result) // Patch to accurately reflect info for socket connections
    {
        if (!MatchmakingHandler.RunningOnSockets) return true;

        __result = NetworkUtils.LidgrenData.ServerConnection.Status == NetConnectionStatus.Connected;
        return false;
    }
}