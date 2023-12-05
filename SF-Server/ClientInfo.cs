using System.Net;
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
    public PositionPackage PositionInfo { get; set; }
    public WeaponPackage WeaponInfo { get; set; }
    public float Hp { get; private set; }
    public bool IsAlive { get; private set; }
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
        Hp = 100;
        IsAlive = true;
        PositionInfo = new PositionPackage();
    }

    public void DeductHp(float amount)
    {
        Hp -= amount;
        
        if (Hp <= 0) 
            IsAlive = false;
    }

    public void Revive()
    {
        IsAlive = true;
        Hp = 100;
    }

    public override bool Equals(object obj) => obj is ClientInfo client && Equals(client.Address, Address);
    
    public bool Equals(ClientInfo other) => other is not null && Equals(other.Address, Address);

    public override int GetHashCode() => Address.GetHashCode();

    public override string ToString() 
        => $"\nSteamID: {SteamID}\nName: {Username}\nAddress: {Address}\nAuthTicket: {AuthTicket.ToString().Truncate(10)}" 
           + $"\nPlayerIndex: {PlayerIndex}\nPing: {Ping}";
}