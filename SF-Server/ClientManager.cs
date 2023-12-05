using System.Net;
using Lidgren.Network;

namespace SF_Server;

public class ClientManager
{
    public ClientInfo[] Clients { get; }
    
    public ClientManager(int numClients) => Clients = new ClientInfo[numClients];

    public void AddNewClient(SteamId steamID, string steamUsername, AuthTicket authTicket, IPAddress address)
    {
        var newClient = new ClientInfo(steamID, steamUsername, authTicket, address, GetEmptyPlayerIndex());
        
        if (newClient.PlayerIndex != -1)
        {
            Clients[newClient.PlayerIndex] = newClient;
            Console.WriteLine("Added new client!\n" + newClient);
            return;
        }

        Console.WriteLine("AddNewClient() was called but no client was added with index " + newClient.PlayerIndex + ", bug??");
    }
    
    public void RemoveClient(ClientInfo removedClient)
    {
        for (var i = 0; i < Clients.Length; i++)
        {
            var client = Clients[i];
            
            if (client is not null && client.Equals(removedClient))
            {
                Clients[i] = null; // Frees up spot for new player
                Console.WriteLine("Client removed at index: " + i);
                return;
            }
        }
    }

    public void RemoveDisconnectedClients()
    {
        for (var i = 0; i < Clients.Length; i++)
        {
            var client = Clients[i];
            
            if (client is not null && client.Status == NetConnectionStatus.Disconnected)
                Clients[i] = null; // Frees up spot for new player
        }
    }
    
    private int GetEmptyPlayerIndex()
    {
        for (var i = 0; i < Clients.Length; i++)
            if (Clients[i] is null)
                return i;

        return -1;
    }

    public void PostRoundCleanup()
    {
        foreach (var client in Clients) 
            client.Revive();
    }

    public int GetNumLivingClients() => Clients.Count(client => client.IsAlive);

    public ClientInfo GetClient(IPAddress address) 
        => Clients.FirstOrDefault(player => player is not null && Equals(player.Address, address));

    public ClientInfo GetClient(int playerIndex)
        => Clients[playerIndex];

    public ClientInfo GetClient(SteamId id)
        => Clients.FirstOrDefault(player => player is not null && player.SteamID == id);
}