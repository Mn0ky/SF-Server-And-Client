using System.Text.Json.Serialization;

namespace SF_Server;

public class AuthResponse
{
    [JsonConstructor]
    public AuthResponse(JsonResponse response) => Response = response;
    
    public JsonResponse Response { get; }

    public override string ToString()
    {
        var @params = Response.Params;
        
        return $"\nResult: {@params.Result}\nSteamid: {@params.Steamid}\nOwnersteamid: {@params.Ownersteamid}" +
               $"\nVacbanned: {@params.Vacbanned}\nPublisherbanned: {@params.Publisherbanned}";
    }
    
    public class Params
    {
        [JsonConstructor]
        public Params(string result, string steamid, string ownersteamid, bool vacbanned, bool publisherbanned)
        {
            Result = result;
            Steamid = steamid;
            Ownersteamid = ownersteamid;
            Vacbanned = vacbanned;
            Publisherbanned = publisherbanned;
        }    
        public string Result { get; }
        public string Steamid { get; }
        public string Ownersteamid { get; }
        public bool Vacbanned { get; }
        public bool Publisherbanned { get; }
    }

    public class JsonResponse
    {
        [JsonConstructor]
        public JsonResponse(Params @params) => Params = @params;

        public Params Params { get; }
    }
}