using SF_Server;

var port = 1337; // Default port
var steamWebAPIToken = ""; // Invalid token
var hostSteamId = 0UL; // Invalid steamid
        
// Parsing parameters passed to server
        
for (var i = 0; i < args.Length; i+=2)
{
    var parameter = args[i];
    if (i + 1 >= args.Length) // If param does not have arg after it stop the loop
        break;

    bool didParse;
    switch (parameter)
    {
        case "--port":
            didParse = int.TryParse(args[i + 1], out port);
            if (!didParse)
            {
                Console.WriteLine("Failed to parse port parameter, using :1337 as default...");
                break;
            }
            Console.WriteLine($"Hosting on port: {port}");
            break;
        case "--steam_web_api_token":
            steamWebAPIToken = args[i + 1];
            if (string.IsNullOrWhiteSpace(steamWebAPIToken))
            {
                Console.WriteLine("Steam Web API token argument parsed as empty! Exiting...");
                return;
            }
            Console.WriteLine($"Using Steam Web API token: {steamWebAPIToken.Truncate(4)}");
            break;
        case "--host_steamid":
            didParse = ulong.TryParse(args[i + 1], out hostSteamId);
            if (!didParse || hostSteamId == 0)
            {
                Console.WriteLine("Failed to parse host's SteamID parameter, exiting...");
                return;
            }
            Console.WriteLine($"Using {hostSteamId} as the SteamID for Steam Web API Requests...");
            break;
        default:
            Console.WriteLine($"Unrecognized server arg \"{parameter}\", ignoring...");
            break;
    }
}

if (string.IsNullOrEmpty(steamWebAPIToken) || hostSteamId == 0)
{
    Console.WriteLine("Server cannot start without specifying the parameters '--steam_web_api_token' and '--host_steamid'");
    Environment.Exit(1);
}    

var server = new Server(port, steamWebAPIToken, new SteamId(hostSteamId));
var serverStarted = server.Start();
        
if (!serverStarted)
{
    Console.WriteLine("Server failed to start on port: " + port);
    Environment.Exit(1);
}
        
while (true)
    server.Update();