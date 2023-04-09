using System.Text.Json.Serialization;

namespace SF_Server;

public class ProfileSummaryResponse
{
    [JsonConstructor]
    public ProfileSummaryResponse(JsonResponse response) => Response = response;
    
    public JsonResponse Response { get; }

    public class Player
    {
        [JsonConstructor]
        public Player(string personaname) => Personaname = personaname;

        public string Personaname { get; }
    }

    public class JsonResponse
    {
        [JsonConstructor]
        public JsonResponse(List<Player> players) => Players = players;

        public List<Player> Players { get; } // Json will fail to unmarshal if list is made IReadOnlyList
    }
}