using PluviaeCanticum.Tracks;
using Tomlet.Attributes;

namespace PluviaeCanticum;

public class Settings
{
    public string CustomTracksPath { get; set; } = string.Empty;

    [TomlNonSerialized] 
    public Track[] Tracks { get; set; } = [];

    public SceneTrack[] SceneTracks { get; set; } = [];
    public TeleporterTrack[] TeleporterTracks { get; set; } = [];
    public BossTrack[] BossTracks { get; set; } = [];
}