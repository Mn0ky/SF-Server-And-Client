using System.Net;
using System.Text.Json;
using Lidgren.Network;

namespace SF_Server;

public class ClientInfo : IEquatable<ClientInfo>
{
    public SteamId SteamID { get; }
    public string Username { get; }
    //public NetConnection Peer { get; }
    public IPAddress Address { get; }
    public NetConnectionStatus Status { get; set; }
    public int PlayerIndex { get; }
    public int Ping { get; set; }
    public AuthTicket AuthTicket { get; }

    public ClientInfo(SteamId steamID, string steamUsername/*, NetConnection peer*/, AuthTicket authTicket, IPAddress address, int playerIndex)
    {
        SteamID = steamID;
        Username = steamUsername;
        //Peer = peer;
        AuthTicket = authTicket;
        Address = address;
        PlayerIndex = playerIndex;
        Ping = 0;
    }

    public override bool Equals(object obj) => obj is ClientInfo client && Equals(client.Address, Address);
    
    public bool Equals(ClientInfo other) => other is not null && Equals(other.Address, Address);

    public override int GetHashCode() => Address.GetHashCode();

    public override string ToString() 
        => $"\nSteamID: {SteamID}\nName: {Username}\nAddress: {Address}\nAuthTicket: {AuthTicket.ToString().Truncate(10)}" 
           + $"\nPlayerIndex: {PlayerIndex}\nPing: {Ping}";

    // private void OnPersonaStateChangeHandler(PersonaStateChange_t callback)
    // {
    //     Console.WriteLine("OnPersonaStateChangeHandler has been called!!");
    //     Username = SteamFriends.GetFriendPersonaName(SteamID);
    // }
}