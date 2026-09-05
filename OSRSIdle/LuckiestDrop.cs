namespace OSRSIdle;

/// <summary>Persistent record of the rarest successful drop on this character.</summary>
public sealed class LuckiestDrop
{
    public string ItemName { get; set; } = "";
    public long Attempts { get; set; }
    public double Chance { get; set; }
    public string Source { get; set; } = "";

    public bool IsValid => !string.IsNullOrWhiteSpace(ItemName) && Chance > 0;
}
