using HarmonyLib;

namespace SF_Lidgren;

internal static class PauseManagerPatches
{
    public static void Patches(Harmony harmonyInstance)
    {
        var restartMethod = AccessTools.Method(typeof(PauseManager), "Restart"); // Main menu button method
        var restartMethodPrefix = new HarmonyMethod(typeof(PauseManagerPatches)
            .GetMethod(nameof(RestartMethodPrefix)));

        harmonyInstance.Patch(restartMethod, prefix: restartMethodPrefix);
    }

    public static void RestartMethodPrefix()
    {
        if (!MatchmakingHandler.RunningOnSockets) // If playing vanilla p2p
            return;
        
        NetworkUtils.ExitServer(false);
    }
}