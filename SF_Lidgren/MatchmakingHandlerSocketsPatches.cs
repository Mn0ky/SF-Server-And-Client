using System;
using System.Reflection;
using HarmonyLib;
using Lidgren.Network;
using Steamworks;
using UnityEngine;

namespace SF_Lidgren;

public static class MatchmakingHandlerSocketsPatches
{
    public static void Patches(Harmony harmonyInstance)
    {
        var readMessageMethod = AccessTools.Method(typeof(MatchMakingHandlerSockets), "ReadMessage");
        var readMessageMethodPrefix = new HarmonyMethod(typeof(MatchmakingHandlerSocketsPatches)
            .GetMethod(nameof(ReadMessageMethodPrefix)));

        var joinServerMethod = AccessTools.Method(typeof(MatchMakingHandlerSockets), nameof(MatchMakingHandlerSockets.JoinServer));
        var joinServerMethodPrefix = new HarmonyMethod(typeof(MatchmakingHandlerSocketsPatches)
            .GetMethod(nameof(JoinServerMethodPrefix)));
        
        var joinServerAtMethod = AccessTools.Method(typeof(MatchMakingHandlerSockets), nameof(MatchMakingHandlerSockets.JoinServerAt));
        var joinServerAtMethodPrefix = new HarmonyMethod(typeof(MatchmakingHandlerSocketsPatches)
            .GetMethod(nameof(JoinServerMethodAtPrefix)));

        harmonyInstance.Patch(readMessageMethod, prefix: readMessageMethodPrefix);
        harmonyInstance.Patch(joinServerMethod, prefix: joinServerMethodPrefix);
        harmonyInstance.Patch(joinServerAtMethod, prefix: joinServerAtMethodPrefix);
    }
    
    public static bool ReadMessageMethodPrefix(ref bool ___m_Active, ref NetClient ___m_Client, ref NetIncomingMessage __result)
    {
        NetIncomingMessage msg;
        __result = null;
        
        if (!___m_Active) return false;
        if ((msg = ___m_Client.ReadMessage()) == null) return false;

        var channel = msg.SequenceChannel;
        Debug.Log("Msg has channel: " + channel);
        
        if (channel is > -2 and < 2 or > 9)//  Don't want NetworkPlayer updates going through the normal p2p handler
        {
            __result = msg;
            return false;
        }

        Debug.Log("Packet is meant for NetworkPlayer!");
        var isUpdateChannel = channel % 2 == 0; // Whether channel is update or event channel
        int senderPlayerID;
        
        if (isUpdateChannel)
        {
            senderPlayerID = (channel - 2) / 2;
            NetworkUtils.PlayerUpdatePackets[senderPlayerID] = msg;
            return false;   
        }
      
        Console.WriteLine($"Adding msg with channel {channel} to event packets array!");
        senderPlayerID = (channel - 3) / 2;
        NetworkUtils.PlayerEventPackets[senderPlayerID] = msg;
        return false;
    }

    public static bool JoinServerMethodPrefix(ref bool ___m_Active, ref bool ___m_IsServer, ref NetClient ___m_Client,
        ref NetConnection ___m_NetConnection)
    {
        ___m_Active = true;
        ___m_IsServer = false;
        SetRunningOnSockets(true);
        Console.WriteLine("Matchmaking running on sockets?: " + MatchmakingHandler.RunningOnSockets);

        var netPeerConfiguration = new NetPeerConfiguration(Plugin.AppIdentifier);

        netPeerConfiguration.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);

        var netClient = new NetClient(netPeerConfiguration);
        netClient.Start();
        ___m_Client = netClient;
        var discoveredPeer = ___m_Client.DiscoverKnownPeer(TempGUI.Address, TempGUI.Port);
        Debug.Log("Did discover server at address: " + discoveredPeer);
        
        // Client-side auth work: get and send ticket to server for verification
        var ticketByteArray = new byte[1024];
        var ticketHandler = SteamUser.GetAuthSessionTicket(ticketByteArray, ticketByteArray.Length, out var ticketSize);
        Array.Resize(ref ticketByteArray, (int)ticketSize);

        var onConnectMsg = ___m_Client.CreateMessage();
        onConnectMsg.Write(ticketByteArray);

        // Attempt to connect to server
        ___m_NetConnection = ___m_Client.Connect(TempGUI.Address, TempGUI.Port, onConnectMsg);
        NetworkUtils.LidgrenData = new LidgrenData(ticketHandler, ___m_Client, ___m_NetConnection);

        return false;
    }

    public static bool JoinServerMethodAtPrefix() => false;

    private static void SetRunningOnSockets(bool isOnSockets) 
        => AccessTools.Property(typeof(MatchmakingHandler), nameof(MatchmakingHandler.RunningOnSockets))
            .SetValue(null, // obj instance is null because property is static
                isOnSockets,
                BindingFlags.Default,
                null,
                null,
                null!);
}