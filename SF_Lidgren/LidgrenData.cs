using Lidgren.Network;
using Steamworks;

namespace SF_Lidgren;

public class LidgrenData
{
    public HAuthTicket AuthTicketHandler;
    public readonly NetClient LocalClient;
    public readonly NetConnection ServerConnection;

    public LidgrenData(HAuthTicket handler, NetClient localClient, NetConnection serverConnection)
    {
        AuthTicketHandler = handler;
        LocalClient = localClient;
        ServerConnection = serverConnection;
    }    
}