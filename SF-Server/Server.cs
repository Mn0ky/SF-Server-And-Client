using System.Text;
using Lidgren.Network;
using System.Text.Json;

namespace SF_Server;

public class Server
{
    // TODO: Handle graceful server shutdowns (ctrl+c, etc.)
    // TODO: Look at MultiplayerManager.RequestClientInit() for next steps
    // TODO: Rework way packets are received instead of mimicking game (msg.ReadByte() instead of BinaryReader etc.)
    // TODO: (from above) this includes removing that unnecessary steamID
    // TODO: Handle cold exits by clients (directly quiting the game instead of returning to menu)
    // TODO: Possibility that multiple connections can be made by same client, bad!
    
    public string ServerLogPath { get; }
    
    private readonly NetServer _masterServer;
    private readonly ClientManager _clientMgr;
    private readonly string _webApitoken;
    private readonly SteamId _hostSteamId;
    private readonly HttpClient _httpClient;
    private readonly PacketWorker _packetWorker;
    private readonly Random _rand;
    private readonly JsonSerializerOptions _jsonOptions;
    //private readonly List<IPAddress> _approvedIPs;
    private const string LidgrenIdentifier = "monky.SF_Lidgren";
    private const string StickFightAppId = "674940"; 
    private const int MaxPlayerCount = 4; // Hardcoded to 4 for now to remain compatible with base game

    private int NumberOfClients => _masterServer.Connections.Count;

    public Server(int port, string steamWebApiToken, SteamId hostSteamId)
    {
        var config = new NetPeerConfiguration(LidgrenIdentifier)
        {
            Port = port,
            MaximumConnections = MaxPlayerCount
        };

        config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
        config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
        
        var server = new NetServer(config);
        ServerLogPath = Path.Combine(Environment.CurrentDirectory, "debug_log.txt");
        _masterServer = server;
        _webApitoken = steamWebApiToken;
        _hostSteamId = hostSteamId;
        _rand = new Random();
        _httpClient = new HttpClient(); // Perhaps configure SocketsHttpHandler.PooledConnectionLifetime ?
        _packetWorker = new PacketWorker(this);
        _jsonOptions = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};
        _clientMgr = new ClientManager(MaxPlayerCount);
        //_approvedIPs = new List<IPAddress>();
    }

    public bool Start()
    {
        _masterServer.Start();

        Console.WriteLine("Starting up UDP socket server: " + _masterServer.Status);
        if (string.IsNullOrEmpty(_webApitoken))
        {
            Console.WriteLine("Invalid steam web api token, please specify it properly as a program parameter. " +
                              "This is required for user auth so the server won't start without it.");
        }

        return _masterServer.Status == NetPeerStatus.Running;
    }

    public void Close()
    {
        _masterServer.Shutdown("Shutting down server.");
        Console.WriteLine("Server has been shutdown.");
    }

    public void Update()
    {
        var msg = _masterServer.ReadMessage();
        if (msg is null) return;

        switch (msg.MessageType)
        {
            case NetIncomingMessageType.VerboseDebugMessage:
                PrintStatusStr(msg);
                break;
            case NetIncomingMessageType.DebugMessage:
                PrintStatusStr(msg);
                break;
            case NetIncomingMessageType.WarningMessage:
                PrintStatusStr(msg);
                break;
            case NetIncomingMessageType.ErrorMessage:
                PrintStatusStr(msg);
                break;
            case NetIncomingMessageType.Error:
                break;
            case NetIncomingMessageType.StatusChanged:
                OnClientStatusChanged(msg);
                break;
            case NetIncomingMessageType.UnconnectedData:
                break;
            case NetIncomingMessageType.ConnectionApproval:
                OnPlayerRequestingConnection(msg);
                return; // Don't want msg being null in async auth method
            case NetIncomingMessageType.Data:
                _packetWorker.ParseGamePacket(msg);
                break;
            case NetIncomingMessageType.Receipt:
                break;
            case NetIncomingMessageType.DiscoveryRequest:
                OnPlayerDiscovered(msg);
                break;
            case NetIncomingMessageType.DiscoveryResponse:
            case NetIncomingMessageType.NatIntroductionSuccess:
            case NetIncomingMessageType.ConnectionLatencyUpdated:
            default:
                Console.WriteLine("Unhandled type: " + msg.MessageType);
                break;
        }

        Console.WriteLine("Recycling msg with length: " + msg.Data.Length + "\n");
        _masterServer.Recycle(msg);
    }
    
    private void OnPlayerDiscovered(NetIncomingMessage msg)
    {
        var senderEndPoint = msg.SenderEndPoint;
        var response = _masterServer.CreateMessage();
        response.Write("You have discovered Monky's server, greetings!");

        _masterServer.SendDiscoveryResponse(response, senderEndPoint);
        Console.WriteLine("Player discovered, sending response to: " + senderEndPoint.Address);
    }

    private void OnPlayerRequestingConnection(NetIncomingMessage msg)
    {
        if (NumberOfClients == MaxPlayerCount)
        {
            Console.WriteLine("Server is full, refusing connection...");
            msg.SenderConnection.Deny("Server is full, try again later.");
            _masterServer.Recycle(msg);
            return;
        }    
            
        Console.WriteLine("Attempting to auth user...");
        var address = msg.GetSenderIP();
        var client = _clientMgr.GetClient(address);
        
        if (client is not null)
        {
            Console.WriteLine("Client detected as re-connecting, removing it from client list...");
            _clientMgr.RemoveClient(client);
        }

        Console.WriteLine("User has not been authed, server should have received ticket to continue auth process...");
        Task.Run(() => AuthenticateUser(msg)); // Client should always auth when joining even if they've joined before
        
    }

    private void OnClientStatusChanged(NetIncomingMessage msg)
    {
        var newStatus = (NetConnectionStatus)msg.ReadByte();
        var changeReason = msg.ReadString();
        
        Console.WriteLine("Client's status changed: " + newStatus + "\nReason: " + changeReason);
        
        switch (newStatus)
        {
            case NetConnectionStatus.RespondedConnect:
                Console.WriteLine("Number of clients connected is now: " + NumberOfClients);
                return;
            case NetConnectionStatus.Disconnected:
                OnPlayerExit(msg);
                return;
            case NetConnectionStatus.None:
            case NetConnectionStatus.InitiatedConnect:
            case NetConnectionStatus.ReceivedInitiation:
            case NetConnectionStatus.RespondedAwaitingApproval:
            case NetConnectionStatus.Connected:
            case NetConnectionStatus.Disconnecting:
            default:
                return;
        }
    }

    private void OnPlayerExit(NetIncomingMessage msg)
    {
        var exitingPlayer = _clientMgr.GetClient(msg.GetSenderIP());
        if (exitingPlayer is null) return;
        
        Console.WriteLine("Client is leaving: " + exitingPlayer.Username);
        exitingPlayer.Status = NetConnectionStatus.Disconnected;
        _clientMgr.RemoveDisconnectedClients();
    }
    
    private async Task AuthenticateUser(NetIncomingMessage msg)
    {
        
        var senderConnection = msg.SenderConnection;

        // Post to Steam Web API to verify ticket
        var authResult = await VerifyAuthTicketRequest(msg);

        Console.WriteLine("IS PLAYER AUTHED: " + authResult);
        if (!authResult) // Player is not authed
        {
            Console.WriteLine("Player is not authorized by Steam, denying...");
            msg.SenderConnection.Deny("You are not authorized under Steam."); // Client will not join
            _masterServer.Recycle(msg);
            return;
        }

        Console.WriteLine("Player has successfully authed, allowing them to join...");
        //_approvedIPs.Add(senderConnection.RemoteEndPoint.Address);
        senderConnection.Approve(); // Client will join
        _masterServer.Recycle(msg);
        //_masterServer.SendToAll();
    }
    
    // Auth via web request
    private async Task<bool> VerifyAuthTicketRequest(NetIncomingMessage msg)
    {
        if (msg.Data is null) return false;
        
        var authTicket = new AuthTicket(msg.Data);
        Console.WriteLine("Attempting to verify user ticket: " + authTicket);
        
        var authTicketUri = "https://api.steampowered.com//ISteamUserAuth/AuthenticateUserTicket/v1/" +
                            $"?key={_webApitoken}&appid={StickFightAppId}&ticket={authTicket}&steamid={_hostSteamId}";
        Console.WriteLine("auth ticket uri: " + authTicketUri);

        await Task.Delay(1000); // Delay request by 1 second to reduce false positives of a ticket being invalid
        var jsonResponse = await _httpClient.GetStringAsync(authTicketUri);
        Console.WriteLine("Steam auth json response: " + jsonResponse);
        
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(jsonResponse, _jsonOptions);

        if (authResponse?.Response.Params is null) // Client cannot be authed because json was null or "error" was returned
        {
            Console.WriteLine("Auth request returned error, denying connection!!");
            return false;
        }

        var authResponseData = authResponse.Response.Params;
        
        Console.WriteLine("AuthResponse parsed: " + authResponse);

        if (authResponseData is {Result: not "OK", Publisherbanned: true, Vacbanned: true}) // Client cannot be authed
            return false;

        Console.WriteLine("Auth has not returned error, attempting to parse steamID");

        var playerSteamID = new SteamId(ulong.Parse(authResponseData.Steamid));

        if (playerSteamID.IsBadId()) return false; // Double check validity of steamID

        var playerUsername = await FetchSteamUserName(playerSteamID);
        
        _clientMgr.AddNewClient(playerSteamID,
            playerUsername,
            authTicket,
            msg.GetSenderIP());
        
        return true;
    }
    
    private async Task<string> FetchSteamUserName(SteamId clientSteamId)
    {
        var playerSummariesUri = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/" +
                                  $"?key={_webApitoken}&steamids={clientSteamId}";
        
        var jsonResponse = await _httpClient.GetStringAsync(playerSummariesUri);
        var profileSummary = JsonSerializer.Deserialize<ProfileSummaryResponse>(jsonResponse, _jsonOptions);
        
        if (profileSummary is null || profileSummary.Response.Players.Count == 0)
            return "NOT_FOUND";

        return profileSummary.Response.Players[0].Personaname; // The client's steam name
    }

    private static void PrintStatusStr(NetBuffer msg)
        => Console.WriteLine(msg.ReadString());
    
    // *************************************************
    // Methods to be executed involving packets sent to and from game
    // *************************************************
    
    // TODO: Figure out more efficient way to send these so up to 3 msg objects aren't being created for certain packets 
    public void SendPacketToUser(NetConnection user, byte[] data, SfPacketType messageType,
        NetDeliveryMethod sendMethod = NetDeliveryMethod.ReliableOrdered, int channel = 0)
    {
        var msg = _masterServer.CreateMessage(5 + data.Length); // 5 extra bytes for uint timeSent and byte msgType
        msg.Write(uint.MaxValue); // TODO: Figure out valid time later
        msg.Write((byte)messageType); // Packet type
        msg.Write(data);  // packet data
        
        _masterServer.SendMessage(msg, user, NetDeliveryMethod.ReliableOrdered, channel);
    }
    
    public void SendPacketToAllUsers(byte[] data, SfPacketType messageType,
        NetConnection ignoredUser = null, NetDeliveryMethod sendMethod = NetDeliveryMethod.ReliableOrdered,
        int channel = 0)
    {
        var msg = _masterServer.CreateMessage(5 + data.Length); // 5 extra bytes for uint timeSent and byte msgType
        msg.Write(uint.MaxValue); // TODO: Figure out valid time later
        msg.Write((byte)messageType); // Packet type
        msg.Write(data);  // packet data

        // foreach (var b in msg.Data) 
        //     Console.WriteLine(b);
        // Console.WriteLine();
        
        _masterServer.SendToAll(msg, ignoredUser, sendMethod, channel);
    }
    
    public void OnPlayerRequestingIndex(NetConnection user)
    {
        var playerInfo = _clientMgr.GetClient(user.RemoteEndPoint.Address);
        
        Console.WriteLine("This client's index will be: " + playerInfo.PlayerIndex);
        var tempMsg = _masterServer.CreateMessage();
        tempMsg.Write((byte)playerInfo.PlayerIndex);
        tempMsg.Write(playerInfo.SteamID.id);

        SendPacketToAllUsers(tempMsg.Data, SfPacketType.ClientJoined, user);
        tempMsg = _masterServer.CreateMessage();
        
        tempMsg.Write((byte)1); // Client accepted as long as this is '1'
        tempMsg.Write((byte)playerInfo.PlayerIndex);
        tempMsg.Write((byte)0); // Landfall map type
        tempMsg.Write(4); // Int representing number of bytes map has, 4 for single int '0' that signals vanilla
        tempMsg.Write(0); // Map data of int '0' signals lobby map
        
        foreach (var client in _clientMgr.Clients) // Should only be non-null clients
        {
            // Write steamId of clients or if player index is empty signal this with invalid steamId
            tempMsg.Write(client is not null ? client.SteamID.id : 0UL);
            
            // Only actual/non-connecting users from here
            if (client is null || Equals(client.Address, user.RemoteEndPoint.Address)) 
                continue; 
            
            // TODO: Create proper PlayerStats obj
            for (var i = 0; i < 13; i++) tempMsg.Write(0); // Write 0's for 13 empty stats until that's handled
        }
        
        tempMsg.Write(ushort.MinValue); // TODO: Figure this out properly (something weapons)?
        
        // TODO: Figure out NetworkOptions (maps, hp, regen, weaponsSpawn in that order)
        tempMsg.Write((byte)0);
        tempMsg.Write((byte)0);
        tempMsg.Write((byte)0);
        tempMsg.Write((byte)0);

        // foreach (var b in tempMsg.Data) 
        //     Console.Write(b);
        // Console.WriteLine(); 
        
        SendPacketToUser(user, tempMsg.Data, SfPacketType.ClientInit);
    }

    public void OnPlayerRequestingToSpawn(NetConnection user, NetIncomingMessage spawnPosData)
    {
        var tempMsg = _masterServer.CreateMessage();
        //Console.WriteLine("spawnPosData Length: " + spawnPosData.Length);
        tempMsg.Write(spawnPosData.ReadBytes(25)); // Contains player index, spawn pos. vector, and rotation vector
        // TODO: Switch out false with current map type
        tempMsg.Write(false && NumberOfClients > 1); // Changes spawn pos if not on lobby map and more than 1 player

        SendPacketToAllUsers(tempMsg.ReadBytes(26), SfPacketType.ClientSpawned); 
    }

    public void OnPlayerUpdate(NetConnection user, NetIncomingMessage playerUpdateData)
    {
        var client = _clientMgr.GetClient(user.RemoteEndPoint.Address);

        // foreach (var b in playerUpdateData.Data)
        // {
        //     Console.WriteLine(b);
        // }
        
        // 10th and 11th bytes should make up ushort representing the # of projectiles
        // var earlyNumProjectilesBytes = new[] { playerUpdateData.Data[9], playerUpdateData.Data[10] };
        // var earlyNumProjectiles = BitConverter.ToUInt16(earlyNumProjectilesBytes);
        
        // Console.WriteLine("Number of projectiles: " + earlyNumProjectiles);
        
        // We need to send this packet out to the rest of the clients in the server, +1 byte for weapon type
        SendPacketToAllUsers(
            playerUpdateData.PeekBytes(playerUpdateData.Data.Length - 5),
            SfPacketType.PlayerUpdate,
            user,
            NetDeliveryMethod.UnreliableSequenced,
            playerUpdateData.SequenceChannel
            );

        var positionInfo = new PositionPackage
        {
            Position = new Vector3
            {
                Y = playerUpdateData.ReadInt16() / 100f,
                Z = playerUpdateData.ReadInt16() / 100f
            },
            Rotation = new Vector2
            {
                X = playerUpdateData.ReadSByte() / 100f,
                Y = playerUpdateData.ReadSByte() / 100f
            },
            YValue = playerUpdateData.ReadSByte(),
            MovementType = playerUpdateData.ReadByte()
        };

        client.PositionInfo = positionInfo;

        var weaponInfo = new WeaponPackage
        {
            FightState = playerUpdateData.ReadByte()
        };

        var numProjectiles = playerUpdateData.ReadUInt16();
        var projectiles = new ProjectilePackage[numProjectiles];
        
        for (ushort i = 0; i < projectiles.Length; i++)
        {
            projectiles[i] = new ProjectilePackage(new Vector2
                {
                    X = playerUpdateData.ReadInt16(),
                    Y = playerUpdateData.ReadInt16()
                },
                new Vector2
                {
                    X = playerUpdateData.ReadSByte(),
                    Y = playerUpdateData.ReadSByte()
                },
                playerUpdateData.ReadUInt16());
        }
        
        weaponInfo.WeaponType = playerUpdateData.ReadByte();
        client.WeaponInfo = weaponInfo;
        
        //Console.WriteLine("Position info: " + positionInfo);
        //Console.WriteLine("Weapon info: " + weaponInfo);
    }

    public void OnPlayerForceAdded(NetConnection user, NetIncomingMessage damageData)
    {
        // TODO: Add logic for updating anything serverside
        // var client = GetClient(user.RemoteEndPoint.Address);
        
        SendPacketToAllUsers(
            damageData.PeekBytes(damageData.Data.Length - 5),
            SfPacketType.PlayerForceAdded,
            user,
            NetDeliveryMethod.ReliableOrdered,
            damageData.SequenceChannel
        );
    }

    public void OnPlayerTookDamage(NetConnection user, NetIncomingMessage damageData)
    {
        Console.WriteLine("Sending playertookdamage packet...");
        
        SendPacketToAllUsers(
            damageData.PeekBytes(damageData.Data.Length - 5),
            SfPacketType.PlayerTookDamage,
            null,
            NetDeliveryMethod.ReliableOrdered,
            damageData.SequenceChannel
        );
        
        // TODO: Finish logic for updating client's serverside hp
        var attackerClient = _clientMgr.GetClient(user.RemoteEndPoint.Address);
        var damagedClientEventChannel = damageData.SequenceChannel;
        
        var damagedClient = _clientMgr.GetClient((damagedClientEventChannel - 3) / 2);
        var dmgAmount = damageData.ReadFloat();
        damagedClient.DeductHp(dmgAmount);
        
        // TODO: Have server send out map packet--Integrate custom orders and custom maps
        //if (damagedClient.IsAlive || _clientMgr.GetNumLivingClients() != 0) return;
        // Round is over, 1 player left living

        //_clientMgr.PostRoundCleanup();
        
        // Console.WriteLine("Round ended, attempting to change map!");
        // var msg = _masterServer.CreateMessage(); // winner index, maptype, mapdata
        // msg.Write((byte)attackerClient.PlayerIndex);
        // msg.Write(0);
        // msg.Write(_rand.Next(0, 110));
        //
        // SendPacketToAllUsers(
        //     msg.Data,
        //     SfPacketType.MapChange
        // );
    }

    public void OnMapChanged(NetConnection user, NetIncomingMessage mapMsgData)
    {
        SendPacketToAllUsers(
            mapMsgData.PeekBytes(mapMsgData.Data.Length - 5),
            SfPacketType.MapChange,
            null,
            NetDeliveryMethod.ReliableOrdered,
            mapMsgData.SequenceChannel
        );
    }

    public void OnPlayerTalked(NetConnection user, NetIncomingMessage chatMsgData)
    {
        var chatMsgBytes = chatMsgData.PeekBytes(chatMsgData.Data.Length - 5);
        var chatMsg = Encoding.UTF8.GetString(chatMsgBytes);
        var sender = _clientMgr.GetClient(user.RemoteEndPoint.Address);
        
        // TODO: You have been accepted! <--- investigate
        // TODO: Is the server only getting correct message? <--- investigate
        // TODO: Error may stem from using ref keyword on value type parameters in certain patches <--- investigate
        Console.WriteLine($"{sender.Username}: {chatMsg}");
        
        SendPacketToAllUsers(
            chatMsgBytes,
            SfPacketType.PlayerTalked,
            user, // Don't need to transmit the msg to the original sender
            NetDeliveryMethod.ReliableOrdered,
            chatMsgData.SequenceChannel
        );
    }
}