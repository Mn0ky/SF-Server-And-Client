using Lidgren.Network;
using Steamworks;

namespace SF_Lidgren;

public class LidgrenData
{
    public HAuthTicket AuthTicketHandler;
    public NetClient LocalClient;
    public NetConnection ServerConnection;

    public LidgrenData(HAuthTicket handler, NetClient localClient, NetConnection serverConnection)
    {
        AuthTicketHandler = handler;
        LocalClient = localClient;
        ServerConnection = serverConnection;
    }    
}