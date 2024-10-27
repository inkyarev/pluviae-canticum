using Tomlet.Attributes;

namespace PluviaeCanticum.Songs;

public abstract class Song
{
    public string Name { get; set; } = string.Empty;
    [TomlNonSerialized]
    public string FilePath { get; set; } = string.Empty;
    public float Volume { get; set; } = 1f;
    [TomlNonSerialized]
    public virtual int FadeOutPreviousMS { get; set; } = 250;
    [TomlNonSerialized]
    public virtual int FadeInMS { get; set; } = 250;

    [TomlNonSerialized]
    public virtual int SilenceMS { get; set; } = 0;

    public abstract bool MatchesConditions();

    public abstract string GetSelectingSongString();
}