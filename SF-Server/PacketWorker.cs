using Lidgren.Network;

namespace SF_Server;

public class PacketWorker
{
	private Server _server;

	public PacketWorker(Server server) => _server = server;

	public void ParseGamePacket(NetIncomingMessage msg)
    {
        //using var memoryStream = new MemoryStream(msg);
        //using var binaryReader = new BinaryReader(memoryStream);
        
        //uint lastTimeStamp = MultiplayerManager.LastTimeStamp;
        Console.WriteLine("Raw Data length: " + msg.Data.Length);
        var timeSent = msg.ReadUInt32(); // <--- Time packet was sent
        Console.WriteLine("Packet sent at time: " + timeSent);
        
        var msgType = (SfPacketType)msg.ReadByte();
        Console.WriteLine("Parsed StickFight packet of type: " + msgType);

        //if (num < lastTimeStamp) Console.WriteLine("Packet is obsolete!");
        //var data = msg.ReadBytes(msg.Data.Length - 6); // Count timeSent, msgType, and 1 for offset byte
        //Console.WriteLine("Data length: " + msg.Data.Length);
        ExecutePacketData(msg, msgType, msg.SenderConnection);
    }

    public void ExecutePacketData(NetIncomingMessage msg, SfPacketType messageType, NetConnection user)
    {
        switch (messageType)
		{
		case SfPacketType.Ping:
			if (msg.Data.Length > 0)
                //this.SendP2PPacketToUser(steamIdRemote, data, PacketWorker.SfPacketType.PingResponse, EP2PSend.k_EP2PSendReliable, 0);
                return;
            return;
        case SfPacketType.PingResponse:
            //PingHandler.PingMessageRecieved(steamIdRemote.m_SteamID, data);
			return;
		case SfPacketType.ClientJoined:
			//this.mNetworkHandler.OnClientJoined(data);
			return;
		case SfPacketType.ClientRequestingAccepting:
			_server.SendPacketToUser(user, Array.Empty<byte>(), SfPacketType.ClientAccepted);
			return;
		case SfPacketType.ClientAccepted:
			//this.mNetworkHandler.OnClientAcceptedByServer();
			return;
		case SfPacketType.ClientInit:
			//_server.OnInitFromServer(data);
			return;
		case SfPacketType.ClientRequestingIndex:
			// this.SendMessageToAllClients(array, P2PPackageHandler.MsgType.ClientJoined, true, 0UL, EP2PSend.k_EP2PSendReliable, 0);
			_server.OnPlayerRequestingIndex(user);
			return;
		case SfPacketType.ClientRequestingToSpawn:
			_server.OnPlayerRequestingToSpawn(user, msg);
			return;
		case SfPacketType.PlayerUpdate:
			_server.OnPlayerUpdate(user, msg);
			return;
		case SfPacketType.ClientSpawned:
			//this.mNetworkHandler.OnPlayerSpawned(data);
			return;
		case SfPacketType.ClientReadyUp:
			//this.mNetworkHandler.OnClientReadyUp(data);
			return;
		case SfPacketType.MapChange:
			//this.mNetworkHandler.OnMapChanged(data);
			return;
		case SfPacketType.WeaponSpawned:
			//this.mNetworkHandler.OnWeaponSpawned(data);
			return;
		case SfPacketType.ClientRequestWeaponDrop:
			//this.mNetworkHandler.OnPlayerRequestingWeaponDrop(data);
			return;
		case SfPacketType.WeaponDropped:
			//this.mNetworkHandler.OnWeaponDropped(data);
			return;
		case SfPacketType.WeaponWasPickedUp:
			//this.mNetworkHandler.OnWeaponWasPickedUp(data);
			return;
		case SfPacketType.ClientRequestingWeaponPickUp:
			//this.mNetworkHandler.OnPlayerRequestingWeaponPickUp(data);
			return;
		case SfPacketType.ObjectSpawned:
			//this.mNetworkHandler.OnObjectSpawned(data);
			return;
		case SfPacketType.GroundWeaponsInit:
			//this.mNetworkHandler.OnGroundWeaponsInit(data);
			return;
		case SfPacketType.MapInfo:
			//this.mNetworkHandler.OnMapInfoRecieved(data);
			return;
		case SfPacketType.MapInfoSync:
			//this.mNetworkHandler.OnMapDataRecieved(data);
			return;
		case SfPacketType.WorkshopMapsLoaded:
			//this.mNetworkHandler.OnNewWorkshopMapsRecieved(data);
			return;
		case SfPacketType.StartMatch:
			//this.mNetworkHandler.OnMatchStart(data);
			return;
		case SfPacketType.OptionsChanged:
			//OptionsHolder.NetworkOptionsChanged(data);
			return;
		case SfPacketType.KickPlayer:
			//this.mNetworkHandler.OnKicked(data);
			return;
        default:
            Console.WriteLine("Message type: " + messageType + " Is not setup!!!");
            return;
        }
    }
}

public enum SfPacketType : byte
{
	Ping,
	PingResponse,
	ClientJoined,
	ClientRequestingAccepting,
	ClientAccepted,
	ClientInit,
	ClientRequestingIndex,
	ClientRequestingToSpawn,
	ClientSpawned,
	ClientReadyUp,
	PlayerUpdate,
	PlayerTookDamage,
	PlayerTalked,
	PlayerForceAdded,
	PlayerForceAddedAndBlock,
	PlayerLavaForceAdded,
	PlayerFallOut,
	PlayerWonWithRicochet,
	MapChange,
	WeaponSpawned,
	WeaponThrown,
	RequestingWeaponThrow,
	ClientRequestWeaponDrop,
	WeaponDropped,
	WeaponWasPickedUp,
	ClientRequestingWeaponPickUp,
	ObjectUpdate,
	ObjectSpawned,
	ObjectSimpleDestruction,
	ObjectInvokeDestructionEvent,
	ObjectDestructionCollision,
	GroundWeaponsInit,
	MapInfo,
	MapInfoSync,
	WorkshopMapsLoaded,
	StartMatch,
	ObjectHello,
	OptionsChanged,
	KickPlayer
}