using System.Linq;
using UnityEngine.SceneManagement;

namespace PluviaeCanticum.Songs;

public class TeleporterSong : SceneSong
{
    public TeleporterState TeleporterStatePlayedAt { get; set; }
    public override bool MatchesConditions()
    {
        return FilePath != string.Empty && 
               ScenesPlayedAt.Any(scene => scene == SceneManager.GetActiveScene().name) &&
               PluviaeCanticumPlugin.CurrentBoss.Equals(Boss.None) &&
               PluviaeCanticumPlugin.CurrentTeleporterState == TeleporterStatePlayedAt;
    }
    
    public override string GetSelectingSongString()
    {
        return TeleporterStatePlayedAt switch
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