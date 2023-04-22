using Lidgren.Network;
using Steamworks;

namespace SF_Lidgren;

public static class NetworkUtils
{
    public static LidgrenData LidgrenData;
    // TODO: Switch array below for List<byte[]> later for better flexibility?
    public static NetIncomingMessage[] NetworkPlayerPackets = new NetIncomingMessage[4]; // For holding packets meant for the NetworkPlayer class
    public static readonly byte[] EmptyByteArray = new byte[0];

    public static void SendPacketToServer(byte[] data, P2PPackageHandler.MsgType messageType,
        NetDeliveryMethod sendMethod = NetDeliveryMethod.ReliableOrdered, int channel = 0)
    {
        //Debug.Log("Sending packet with data length: " + data.Length);
        var msg = LidgrenData.LocalClient.CreateMessage(); // 5 extra bytes for uint timeSent and byte msgType
        msg.Write(SteamUtils.GetServerRealTime()); // time sent
        msg.Write((byte)messageType); // Packet type
        msg.Write(data);  // packet data
        //Debug.Log("Sending message with data length: " + msg.Data.Length);

        LidgrenData.LocalClient.SendMessage(msg, LidgrenData.ServerConnection, NetDeliveryMethod.ReliableOrdered, channel);
    }
}