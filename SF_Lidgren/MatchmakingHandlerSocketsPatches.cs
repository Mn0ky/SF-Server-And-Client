using System;
using HarmonyLib;
using Lidgren.Network;
using Steamworks;
using UnityEngine;

namespace SF_Lidgren;

public static class MatchmakingHandlerSocketsPatches
{
    public static LidgrenData ImportantData;

    public static void Patches(Harmony harmonyInstance)
    {
        var joinServerMethod = AccessTools.Method(typeof(MatchMakingHandlerSockets), nameof(MatchMakingHandlerSockets.JoinServer));
        var joinServerMethodPrefix = new HarmonyMethod(typeof(MatchmakingHandlerSocketsPatches)
            .GetMethod(nameof(JoinServerMethodPrefix)));
        
        harmonyInstance.Patch(joinServerMethod, prefix: joinServerMethodPrefix);
    }

    public static bool JoinServerMethodPrefix(ref bool ___m_Active, ref bool ___m_IsServer, ref NetClient ___m_Client,
        ref NetConnection ___m_NetConnection)
    {
        ___m_Active = true;
        ___m_IsServer = false;
        
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
        ImportantData = new LidgrenData(ticketHandler, ___m_Client, ___m_NetConnection);
        var connection = ImportantData.ServerConnection;
        Debug.Log("Connection status after attempting to join: " + connection.Status);
        
        return false;
    }
}