namespace PluviaeCanticum.Tracks;

public class BossTrack : Track
{
    public string BossName { get; set; }
    public int PhasePlayedAt { get; set; }
    
    public override bool MatchesConditions()
    {
        return FilePath != string.Empty &&
               PluviaeCanticumPlugin.CurrentBoss.Name == BossName &&
               PluviaeCanticumPlugin.CurrentBoss.Phase == PhasePlayedAt;
    }

    public override string GetSelectingTrackString()
    {
        return PhasePlayedAt switch
        {
            1 => $"Started a bossfight against {BossName}",
            -1 => $"Ended a bossfight against {BossName}",
            _ => $"Transitioned the bossfight against {BossName} to phase {PhasePlayedAt}"
        };
    }
}