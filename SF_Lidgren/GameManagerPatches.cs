using System;
using HarmonyLib;
using UnityEngine;

namespace SF_Lidgren;

public static class GameManagerPatches
{
    public static void Patches(Harmony harmonyInstance)
    {
        var killPlayerMethod = AccessTools.Method(typeof(GameManager), nameof(GameManager.KillPlayer));
        var killPlayerMethodPrefix = new HarmonyMethod(typeof(GameManagerPatches)
            .GetMethod(nameof(KillPlayerMethodPrefix)));
        
        //harmonyInstance.Patch(killPlayerMethod, prefix: killPlayerMethodPrefix);
    }
    
    // TODO: Switch this to transpiler + postfix (for stats) at some point
    public static bool KillPlayerMethodPrefix(ref Controller playerToKill, ref Crown ___crown, ref LevelSelection ___levelSelector, GameManager __instance)
    {
        if (!MatchmakingHandler.RunningOnSockets) return true;
        
        if (__instance.playersAlive.Contains(playerToKill)) 
            __instance.playersAlive.Remove(playerToKill);

        if (playerToKill.damager != null && !playerToKill.damager.isAI)
        {
            if (___crown.crownBarrer == playerToKill) 
                ___crown.SetNewKing(playerToKill.damager, false);
            
            playerToKill.damager.OnKilledEnemy(playerToKill);
        }

        var numAlive = 0;
        Controller curController = null;
        foreach (var controller in __instance.playersAlive)
        {
            if (controller == null || controller.GetComponent<CharacterInformation>().isDead) 
                continue;
            
            curController = controller;
            numAlive++;
        }
        
        if (numAlive <= 1)
        {
            Console.WriteLine("Less than 1 player is alive, ending round!");
            if (MatchmakingHandler.IsNetworkMatch)
            {
                if (MultiplayerManager.IsServer || MatchmakingHandler.RunningOnSockets)
                {
                    // TODO: Fix this from break sometimes
                    var nextLevel = ___levelSelector.GetNextLevel();
                    Debug.Log("next level is: " + (MapType)nextLevel.MapType + " with index: " + BitConverter.ToInt32(nextLevel.MapData, 0));
                    var b = numAlive != 0 ? (byte)curController!.GetComponent<NetworkPlayer>().NetworkSpawnID : byte.MaxValue;
                    var lastWinnerSetter = AccessTools.PropertySetter(typeof(GameManager), nameof(GameManager.LastWinner));
                    lastWinnerSetter.Invoke(__instance, new object[] {curController});
                    
                    Debug.Log("CALLING CHANGE MAP!!!");
                    __instance.mMultiplayerManager.ChangeMap(nextLevel, b);
                    //var flag = __instance.lastMapNumber.MapType == 2;
                    //GameManager.m_AnalyticsTrigger.OnMatchEnd(true, flag);
                }
            }
            //else
                //__instance.AllButOnePlayersDied();
        }
        
        playerToKill.OnDeath();
        return false;
    }
}