using HarmonyLib;
using Landfall.Network.Sockets;

namespace SF_Lidgren;

public static class MultiplayerManagerSocketsPatches
{
    public static void Patches(Harmony harmonyInstance)
    {
        var requestClientInitMethod = AccessTools.Method(typeof(MultiplayerManagerSockets), "RequestClientInit");
        var requestClientInitMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerSocketsPatches)
            .GetMethod(nameof(RequestClientInitMethodPrefix)));

        harmonyInstance.Patch(requestClientInitMethod, prefix: requestClientInitMethodPrefix);
    }

    public static bool RequestClientInitMethodPrefix()
    {
        if (!MatchmakingHandler.RunningOnSockets) return true; // Not using dedicated server, run as normal
        
        P2PPackageHandler.Instance.SendSocketP2PPacketToUser(NetworkUtils.LidgrenData.ServerConnection,
            NetworkUtils.EmptyByteArray,
            P2PPackageHandler.MsgType.ClientAccepted);

        return false;
    }
    
    
}