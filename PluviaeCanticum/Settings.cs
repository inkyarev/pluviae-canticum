using PluviaeCanticum.Songs;
using Tomlet.Attributes;

namespace PluviaeCanticum;

public class Settings
{
    public string CustomSongsPath { get; set; } = string.Empty;

    [TomlNonSerialized] 
    public Song[] Songs { get; set; } = [];

    public SceneSong[] SceneSongs { get; set; } = [];
    public TeleporterSong[] TeleporterSongs { get; set; } = [];
    public BossSong[] BossSongs { get; set; } = [];
}