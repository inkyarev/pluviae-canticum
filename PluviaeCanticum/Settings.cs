using PluviaeCanticum.Tracks;
using Tomlet.Attributes;

namespace PluviaeCanticum;

public class Settings
{
    public string CustomTracksPath { get; set; } = string.Empty;

    [TomlNonSerialized] 
    public Track[] Tracks { get; set; } = [];

    [TomlProperty("SceneTrack")]
    public SceneTrack[] SceneTracks { get; set; } = [];
    [TomlProperty("TeleporterTrack")]
    public TeleporterTrack[] TeleporterTracks { get; set; } = [];
    [TomlProperty("BossTrack")]
    public BossTrack[] BossTracks { get; set; } = [];
}