using System;
using System.IO;
using HarmonyLib;
using MonoMod.Utils;
using Steamworks;
using UnityEngine;

namespace SF_Lidgren;

public class NetworkPlayerPatches
{
    private static FastReflectionDelegate _syncClientState;
    
    public static void Patches(Harmony harmonyInstance)
    {
        var listenForPositionPackagesMethod = AccessTools.Method(typeof(NetworkPlayer), "ListenForPositionPackages");
        var listenForPositionPackagesMethodPrefix = new HarmonyMethod(typeof(NetworkPlayerPatches)
            .GetMethod(nameof(ListenForPositionPackagesMethodPrefix)));
        
        var initNetworkSpawnIDMethod = AccessTools.Method(typeof(NetworkPlayer), nameof(NetworkPlayer.InitNetworkSpawnID));
        var initNetworkSpawnIDMethodPostfix = new HarmonyMethod(typeof(NetworkPlayerPatches)
            .GetMethod(nameof(InitNetworkSpawnIDMethodPostfix)));

        harmonyInstance.Patch(initNetworkSpawnIDMethod, postfix: initNetworkSpawnIDMethodPostfix);
        harmonyInstance.Patch(listenForPositionPackagesMethod, prefix: listenForPositionPackagesMethodPrefix);
    }
    
    // Be able to act on PlayerUpdate packet without using reflection to invoke SyncClientState() every time
    public static void InitNetworkSpawnIDMethodPostfix(ref ushort networkSpawnID, NetworkPlayer __instance)
    {
        if (!MatchmakingHandler.RunningOnSockets || networkSpawnID != GameManager.Instance.mMultiplayerManager.LocalPlayerIndex) 
            return;

        var syncClientStateMethod = AccessTools.Method(typeof(NetworkPlayer), "SyncClientState");
        _syncClientState = syncClientStateMethod.CreateFastDelegate();
        //_syncClientState = (Action<byte[]>)Delegate.CreateDelegate(typeof(Action<byte[]>), syncClientStateMethod);
    }
    
    // TODO: Cannot be implemented this way due to the queue being used for messages in NetPeer
    public static bool ListenForPositionPackagesMethodPrefix(NetworkPlayer __instance, ref bool ___mIsActive,
        ref bool ___mHasRecievedFirstPackage, ref ushort ___mNetworkSpawnID)
    {
        if (!MatchmakingHandler.RunningOnSockets) return true;
        if (!___mIsActive && ___mHasRecievedFirstPackage) return false;

        var posMsg = NetworkUtils.NetworkPlayerPackets[___mNetworkSpawnID];
        if (posMsg?.Data is null) return false;

        var timeSent = posMsg.ReadUInt32();
        var msgType = (P2PPackageHandler.MsgType)posMsg.ReadByte();

        // if (timeSent < MultiplayerManager.LastTimeStamp) // TODO: Implement logic for timeSent?
        //     Debug.LogWarning("Packet Is obsolete, throwing away! Of TYPE: " + msgType);

        if (msgType != P2PPackageHandler.MsgType.PlayerUpdate)
        {
            Debug.LogError("Invalid NetworkPlayer Messagetype: " + msgType);
            return false;
        }

        Console.WriteLine("Calling syncClientState()");
        _syncClientState(__instance, posMsg.ReadBytes(posMsg.Data.Length - 5));
        NetworkUtils.LidgrenData.LocalClient.Recycle(posMsg);

        return false;
    }
}