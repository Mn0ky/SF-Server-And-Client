using HarmonyLib;
using Lidgren.Network;
using Steamworks;

namespace SF_Lidgren;

public static class P2PPackageHandlerPatch
{
    public static void Patches(Harmony harmonyInstance)
    {
        var sendP2PPacketToUserMethod = AccessTools.Method(typeof(P2PPackageHandler),
            nameof(P2PPackageHandler.SendP2PPacketToUser), new[]
        {
            typeof(CSteamID),
            typeof(byte[]),
            typeof(P2PPackageHandler.MsgType),
            typeof(EP2PSend),
            typeof(int)
        });

        var sendP2PPacketToServerMethod = AccessTools.Method(typeof(P2PPackageHandler),
            nameof(P2PPackageHandler.SendP2PPacketToServer));
        var sendP2PPacketToUserMethodPrefix = new HarmonyMethod(typeof(P2PPackageHandlerPatch)
            .GetMethod(nameof(SendP2PPacketToUserMethodPrefix)));
        
        harmonyInstance.Patch(sendP2PPacketToUserMethod, prefix: sendP2PPacketToUserMethodPrefix);
        harmonyInstance.Patch(sendP2PPacketToServerMethod, prefix: sendP2PPacketToUserMethodPrefix);
    }

    public static bool SendP2PPacketToUserMethodPrefix(ref byte[] data, ref P2PPackageHandler.MsgType messageType,
        ref EP2PSend sendMethod, ref int channel)
    {
        if (!MatchmakingHandler.RunningOnSockets) return true; // Client is using steam networking

        var lidgrenDeliveryMethodEquiv = sendMethod switch
        {
            EP2PSend.k_EP2PSendUnreliable => NetDeliveryMethod.UnreliableSequenced,
            EP2PSend.k_EP2PSendUnreliableNoDelay => NetDeliveryMethod.Unreliable,
            EP2PSend.k_EP2PSendReliable => NetDeliveryMethod.ReliableOrdered,
            EP2PSend.k_EP2PSendReliableWithBuffering => NetDeliveryMethod.ReliableUnordered,
            _ => NetDeliveryMethod.ReliableOrdered
        };

        NetworkUtils.SendPacketToServer(data, messageType, lidgrenDeliveryMethodEquiv, channel);
        return false;
    }
}