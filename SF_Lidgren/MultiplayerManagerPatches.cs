using System;
using HarmonyLib;
using Lidgren.Network;
using Steamworks;

namespace SF_Lidgren;

public class MultiplayerManagerPatches
{
    public static void Patch(Harmony harmonyInstance)
    {
        // TODO: Number of players is hardcoded in method before this, perhaps change this later...
        
        var requestClientInitMethod = AccessTools.Method(typeof(MultiplayerManager), "RequestClientInit");
        var requestClientInitMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(RequestClientInitMethodPrefix)));
        
        var onClientAcceptedByServerMethod = AccessTools.Method(typeof(MultiplayerManager), "OnClientAcceptedByServer");
        var onClientAcceptedByServerMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(OnClientAcceptedByServerMethodPrefix)));
        
        var onInitFromServerMethod = AccessTools.Method(typeof(MultiplayerManager), "OnInitFromServer");
        var onInitFromServerMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(OnInitFromServerMethodPrefix)));
        
        var onPlayerSpawnedMethod = AccessTools.Method(typeof(MultiplayerManager), nameof(MultiplayerManager.OnPlayerSpawned));
        var onPlayerSpawnedMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(OnPlayerSpawnedMethodPrefix)));
        
        var sendMessageToAllClientsMethod = AccessTools.Method(typeof(MultiplayerManager), "SendMessageToAllClients");
        var sendMessageToAllClientsMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(SendMessageToAllClientsMethodPrefix)));
        
        harmonyInstance.Patch(requestClientInitMethod, prefix: requestClientInitMethodPrefix);
        harmonyInstance.Patch(onClientAcceptedByServerMethod, prefix: onClientAcceptedByServerMethodPrefix);
        harmonyInstance.Patch(onInitFromServerMethod, prefix: onInitFromServerMethodPrefix);
        harmonyInstance.Patch(sendMessageToAllClientsMethod, prefix: sendMessageToAllClientsMethodPrefix);
        harmonyInstance.Patch(onPlayerSpawnedMethod, prefix: onPlayerSpawnedMethodPrefix);
    }

    public static bool RequestClientInitMethodPrefix()
    {
        if (!MatchmakingHandler.RunningOnSockets) return true;

        NetworkUtils.SendPacketToServer(NetworkUtils.EmptyByteArray,
            P2PPackageHandler.MsgType.ClientRequestingAccepting,
            NetDeliveryMethod.ReliableOrdered,
            -1);

        return false;
    }
    
    // TODO: Support multiple players on same device?
    public static bool OnClientAcceptedByServerMethodPrefix(ref bool ___mHasBeenAcceptedFromServer) 
    {
        if (!MatchmakingHandler.RunningOnSockets) return true;
        ___mHasBeenAcceptedFromServer = true;
        
        NetworkUtils.SendPacketToServer(NetworkUtils.EmptyByteArray, P2PPackageHandler.MsgType.ClientRequestingIndex);
        return false;
    }

    public static void OnInitFromServerMethodPrefix(ref ConnectedClientData[] ___mConnectedClients)
    {
        if (!MatchmakingHandler.RunningOnSockets) return;

        ___mConnectedClients = new ConnectedClientData[4]; // Client list appears to be empty otherwise
    }
    
    // TODO: Investigate and understand why this happens?
    public static void OnPlayerSpawnedMethodPrefix(ref byte[] data)
    {
        if (!MatchmakingHandler.RunningOnSockets) return;
   
        Console.WriteLine("Looking at spawn flag byte: " + data[25]);

        // Sometimes spawnPosition flag is random byte value instead of bool, if this is the case default to 0
        if (data[25] > 1) data[25] = 0;
    }
    
    // Should only be sending packets to one place: the server
    public static bool SendMessageToAllClientsMethodPrefix(ref byte[] data, ref P2PPackageHandler.MsgType type,
        ref EP2PSend sendMethod, ref int channel)
    {
        if (!MatchmakingHandler.RunningOnSockets) return true; // Client is using steam networking

        var lidgrenDeliveryMethodEquiv = sendMethod switch
        {
            EP2PSend.k_EP2PSendUnreliable => NetDeliveryMethod.Unreliable,
            EP2PSend.k_EP2PSendUnreliableNoDelay => NetDeliveryMethod.Unreliable,
            EP2PSend.k_EP2PSendReliable => NetDeliveryMethod.ReliableOrdered,
            EP2PSend.k_EP2PSendReliableWithBuffering => NetDeliveryMethod.ReliableUnordered,
            _ => NetDeliveryMethod.ReliableOrdered
        };

        NetworkUtils.SendPacketToServer(data, type, lidgrenDeliveryMethodEquiv, channel);
        return false;
    }
}