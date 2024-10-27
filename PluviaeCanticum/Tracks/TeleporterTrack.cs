using System.Linq;
using UnityEngine.SceneManagement;

namespace PluviaeCanticum.Tracks;

public class TeleporterTrack : SceneTrack
{
    private TeleporterState StatePlayedAt { get; set; } = TeleporterState.Charging;
    public override bool MatchesConditions()
    {
        return FilePath != string.Empty && 
               ScenesPlayedAt.Any(scene => scene == SceneManager.GetActiveScene().name) &&
               PluviaeCanticumPlugin.CurrentBoss.Equals(Boss.None) &&
               PluviaeCanticumPlugin.CurrentTeleporterState == StatePlayedAt;
    }
    
    public override string GetSelectingTrackString()
    {
        return StatePlayedAt switch
        {
            TeleporterState.Charging => $"Teleporter activated on '{SceneManager.GetActiveScene().name}' scene",
            TeleporterState.FinishedCharging => $"Teleporter charged on '{SceneManager.GetActiveScene().name}' scene",
            _ => $"Teleporter wasn't interacted with on '{SceneManager.GetActiveScene().name}' scene"
        };
    }
}

public enum TeleporterState
{
    None,
    Charging,
    FinishedCharging
}