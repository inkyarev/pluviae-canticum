using Tomlet.Attributes;

namespace PluviaeCanticum;


public class Boss
{
    public static readonly Boss None = new() { Name = "None" };
    public static readonly Boss Mithrix = new() { Name = "Mithrix" };
    public static readonly Boss FalseSon = new() { Name = "False Son" };
    public static readonly Boss Voidling = new() { Name = "Voidling" };
    public string Name { get; private set; } = string.Empty;
    [TomlNonSerialized]
    public int Phase { get; set; }
}