namespace PluviaeCanticum.Songs;

public class BossSong : Song
{
    public string BossName { get; set; }
    public int PhasePlayedAt { get; set; }
    public override int FadeOutMS { get; set; } = 250;
    public override int FadeInMS { get; set; } = 250;
    public override int SilenceMS { get; set; } = 0;
    
    public override bool MatchesConditions()
    {
        return FilePath != string.Empty &&
               PluviaeCanticumPlugin.CurrentBoss.Name == BossName &&
               PluviaeCanticumPlugin.CurrentBoss.Phase == PhasePlayedAt;
    }

    public override string GetSelectingSongString()
    {
        return PhasePlayedAt switch
        {
            1 => $"Started a bossfight against {BossName}",
            -1 => $"Ended a bossfight against {BossName}",
            _ => $"Transitioned the bossfight against {BossName} to phase {PhasePlayedAt}"
        };
    }
}