using System.Linq;
using UnityEngine.SceneManagement;

namespace PluviaeCanticum.Songs;

public class SceneSong : Song
{
    public string[] ScenesPlayedAt { get; set; } = [];

    public override bool MatchesConditions()
    {
        return FilePath != string.Empty && 
               ScenesPlayedAt.Any(scene => scene == SceneManager.GetActiveScene().name) &&
               PluviaeCanticumPlugin.CurrentBoss.Equals(Boss.None) &&
               PluviaeCanticumPlugin.CurrentTeleporterState == TeleporterState.None;
    }

    public override string GetSelectingSongString()
    {
        return $"Transitioned to '{SceneManager.GetActiveScene().name}' scene";
    }
}