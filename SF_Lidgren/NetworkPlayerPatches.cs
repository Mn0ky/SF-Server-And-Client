using System;
using HarmonyLib;
using Lidgren.Network;
using MonoMod.Utils;
using UnityEngine;

namespace SF_Lidgren;

public class NetworkPlayerPatches
{
    private static FastReflectionDelegate _syncClientState;
    private static FastReflectionDelegate _syncClientHealth;
    private static FastReflectionDelegate _syncClientChat;
    private static FastReflectionDelegate _syncClientForceAdded;
    private static FastReflectionDelegate _syncClientBlockForceAdded;
    private static FastReflectionDelegate _syncClientLavaForceAdded;
    private static FastReflectionDelegate _syncClientFallOut;
    private static FastReflectionDelegate _syncClientWonWithRicochet;
    private static FastReflectionDelegate _syncClientWeaponThrow;

    public static void Patches(Harmony harmonyInstance)
    {
        var initNetworkSpawnIDMethod = AccessTools.Method(typeof(NetworkPlayer), nameof(NetworkPlayer.InitNetworkSpawnID));
        var initNetworkSpawnIDMethodPostfix = new HarmonyMethod(typeof(NetworkPlayerPatches)
            .GetMethod(nameof(InitNetworkSpawnIDMethodPostfix)));
        
        var listenForPositionPackagesMethod = AccessTools.Method(typeof(NetworkPlayer), "ListenForPositionPackages");
        var listenForPositionPackagesMethodPrefix = new HarmonyMethod(typeof(NetworkPlayerPatches)
            .GetMethod(nameof(ListenForPositionPackagesMethodPrefix)));
        
        var listenForEventPackagesMethod = AccessTools.Method(typeof(NetworkPlayer), "ListenForEventPackages");
        var listenForEventPackagesMethodPrefix = new HarmonyMethod(typeof(NetworkPlayerPatches)
            .GetMethod(nameof(ListenForEventPackagesMethodPrefix)));

        harmonyInstance.Patch(initNetworkSpawnIDMethod, postfix: initNetworkSpawnIDMethodPostfix);
        harmonyInstance.Patch(listenForPositionPackagesMethod, prefix: listenForPositionPackagesMethodPrefix);
        harmonyInstance.Patch(listenForEventPackagesMethod, prefix: listenForEventPackagesMethodPrefix);
    }
    
    // Be able to act on PlayerUpdate packet without using reflection to invoke SyncClientState() every time
    public static void InitNetworkSpawnIDMethodPostfix(ref ushort networkSpawnID)
    {
        // Only want these delegates being created once on-join
        if (!MatchmakingHandler.RunningOnSockets || networkSpawnID != GameManager.Instance.mMultiplayerManager.LocalPlayerIndex) 
            return;

        NetworkUtils.PlayerUpdatePackets = new NetIncomingMessage[4];
        NetworkUtils.PlayerEventPackets = new NetIncomingMessage[4];

        _syncClientState = AccessTools.Method(typeof(NetworkPlayer), "SyncClientState").CreateFastDelegate();
        _syncClientHealth = AccessTools.Method(typeof(NetworkPlayer), "SyncClienthealth").CreateFastDelegate();
        _syncClientChat = AccessTools.Method(typeof(NetworkPlayer), "SyncClientChat").CreateFastDelegate();
        _syncClientForceAdded = AccessTools.Method(typeof(NetworkPlayer), "SyncClientForceAdded").CreateFastDelegate();
        _syncClientBlockForceAdded = AccessTools.Method(typeof(NetworkPlayer), "SyncClientBlockForceAdded").CreateFastDelegate();
        _syncClientLavaForceAdded = AccessTools.Method(typeof(NetworkPlayer), "SyncClientLavaForceAdded").CreateFastDelegate();
        _syncClientFallOut = AccessTools.Method(typeof(NetworkPlayer), "SyncClientFallOut").CreateFastDelegate();
        _syncClientWonWithRicochet = AccessTools.Method(typeof(NetworkPlayer), "SyncClientState").CreateFastDelegate();
        _syncClientWeaponThrow = AccessTools.Method(typeof(NetworkPlayer), "SyncClientWeaponThrow").CreateFastDelegate();
    }
    
    // TODO: Cannot be implemented this way due to the queue being used for messages in NetPeer
    public static bool ListenForPositionPackagesMethodPrefix(NetworkPlayer __instance, ref bool ___mIsActive,
        ref bool ___mHasRecievedFirstPackage, ref ushort ___mNetworkSpawnID)
    {
        if (!MatchmakingHandler.RunningOnSockets) return true;
        if (!___mIsActive && ___mHasRecievedFirstPackage) return false;

        var posMsg = NetworkUtils.PlayerUpdatePackets[___mNetworkSpawnID];
        if (posMsg?.Data is null) return false; // Is there a packet available? If not, exit the method

        var timeSent = posMsg.ReadUInt32(); 
        var msgType = (P2PPackageHandler.MsgType)posMsg.ReadByte();

        // if (timeSent < MultiplayerManager.LastTimeStamp) // TODO: Implement logic for timeSent?
        //     Debug.LogWarning("Packet Is obsolete, throwing away! Of TYPE: " + msgType);

        if (msgType != P2PPackageHandler.MsgType.PlayerUpdate)
        {
            Debug.LogError("Invalid update Messagetype: " + msgType);
            return false;
        }

        //Console.WriteLine("Calling syncClientState()");
        _syncClientState(__instance, posMsg.ReadBytes(posMsg.Data.Length - 5));
        NetworkUtils.LidgrenData.LocalClient.Recycle(posMsg);
        return false;
    }

    public static bool ListenForEventPackagesMethodPrefix(NetworkPlayer __instance, ref bool ___mIsActive, 
        ref ushort ___mNetworkSpawnID)
    {
        if (!MatchmakingHandler.RunningOnSockets) return true;
        if (!___mIsActive) return false;
        
        var eventMsg = NetworkUtils.PlayerEventPackets[___mNetworkSpawnID];
        if (eventMsg?.Data is null) return false; // Is there a packet available? If not, exit the method
        
        var timeSent = eventMsg.ReadUInt32();
        var msgType = (P2PPackageHandler.MsgType)eventMsg.ReadByte();
        var data = eventMsg.ReadBytes(eventMsg.Data.Length - 5);
        
        switch (msgType)
        {
            case P2PPackageHandler.MsgType.PlayerTookDamage:
                _syncClientHealth(__instance, data);
                break;
            case P2PPackageHandler.MsgType.PlayerTalked:
                _syncClientChat(__instance, data);
                break;
            case P2PPackageHandler.MsgType.PlayerForceAdded:
                _syncClientForceAdded(__instance, data);
                break;
            case P2PPackageHandler.MsgType.PlayerForceAddedAndBlock:
                _syncClientBlockForceAdded(__instance, data);
                break;
            case P2PPackageHandler.MsgType.PlayerLavaForceAdded:
                _syncClientLavaForceAdded(__instance, data);
                break;
            case P2PPackageHandler.MsgType.PlayerFallOut:
                _syncClientFallOut(__instance, data);
                break;
            case P2PPackageHandler.MsgType.PlayerWonWithRicochet:
                _syncClientWonWithRicochet(__instance, data);
                break;
            case P2PPackageHandler.MsgType.WeaponThrown:
                _syncClientWeaponThrow(__instance, data);
                break;
            // case P2PPackageHandler.MsgType.RequestingWeaponThrow:
            //     if (NetworkPlayer.IsServer)
            //         this.mNetworkManager.OnPlayerThrowWeapon(data, channel);
            default:
                Debug.LogError("Invalid Event Messagetype " + msgType + ", msg has channel: " + eventMsg.SequenceChannel);
                break;
        }
        
        NetworkUtils.LidgrenData.LocalClient.Recycle(eventMsg);
        return false;
    }
}