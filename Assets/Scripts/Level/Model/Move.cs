namespace CastleGuard.Level.Model;

public readonly struct Move
{
    public string From { get; }
    public string To { get; }
    public int WallIndex { get; }

    public Move(string from, string to, int wallIndex)
    {
        From = from;
        To = to;
        WallIndex = wallIndex;
    }
}
