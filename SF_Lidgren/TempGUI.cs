using Steamworks;
using UnityEngine;

namespace SF_Lidgren;

public class TempGUI : MonoBehaviour
{
    private bool _showMenu;
    public static string Address = "localhost";
    public static int Port = 1337;
    private Rect _menuRect = new(Screen.width/2f, Screen.height/2f, 375f, 375f);
    
    private void Start() => Debug.Log("Started GUI in TempGUIManager!");

    private void Update() => _showMenu = true;

    public void OnGUI()
    {
        if (!_showMenu) return;
        _menuRect = GUILayout.Window(1169, _menuRect, JoinWindow, "Join Socket Server");
    }

    private void JoinWindow(int window)
    {
        Address = GUILayout.TextField(Address);
        Port = int.Parse(GUILayout.TextField(Port.ToString()));

        if (GUILayout.Button("Join"))
        {
            Debug.Log("Join button clicked, attempting to join socket server...");
            MatchMakingHandlerSockets.Instance.JoinServer();
        }
        
        if (GUILayout.Button("Exit"))
        {
            Debug.Log("Exit button clicked, attempting to leave socket server...");
            var importantData = NetworkUtils.LidgrenData;
            importantData.LocalClient.Disconnect("I'm leaving >:(");
            SteamUser.CancelAuthTicket(importantData.AuthTicketHandler);
            Debug.Log("Auth ticket has been canceled");
            GameManager.Instance.mMultiplayerManager.OnDisconnected();
        }
        
        if (GUILayout.Button("Status"))
        {
            var importantData = NetworkUtils.LidgrenData;
            Debug.Log("Connection status: " + importantData.ServerConnection.Status);
            //Debug.Log("Connection status: " + importantData.LocalClient.Status);
            Debug.Log("Is inside lobby: " + MatchmakingHandler.Instance.IsInsideLobby);
            Debug.Log("Is network match: " + MatchmakingHandler.IsNetworkMatch);
        }

        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
    }
}