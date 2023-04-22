using BepInEx;
using HarmonyLib;

namespace SF_Lidgren;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string AppIdentifier = "monky.SF_Lidgren";
    
    private void Awake()
    {
        // Plugin startup logic
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        Logger.LogInfo("Preparing patches for SF_Lidgren...");
        
        Harmony harmony = new (AppIdentifier); // Creates harmony instance with identifier
        
        Logger.LogInfo("Applying MatchmakingHandlerSockets patches...");
        MatchmakingHandlerSocketsPatches.Patches(harmony);
        Logger.LogInfo("Applying MatchmakingHandler patch...");
        MatchMakingHandlerPatches.Patches(harmony);
        Logger.LogInfo("Applying MultiplayerManagerSockets Patches...");
        MultiplayerManagerSocketsPatches.Patches(harmony);
        Logger.LogInfo("Applying MultiplayerManager Patch...");
        MultiplayerManagerPatches.Patch(harmony);
        Logger.LogInfo("Applying P2PPackageHandler Patches...");
        P2PPackageHandlerPatch.Patches(harmony);
        Logger.LogInfo("Applying NetworkPlayer Patches...");
        NetworkPlayerPatches.Patches(harmony);
    }
}