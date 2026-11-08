public readonly struct Wall
{
    public string From { get; }
    public string To { get; }

    public Wall(string from, string to)
    {
        From = from;
        To = to;
    }

    public bool Connects(string firstTower, string secondTower)
    {
        return From == firstTower && To == secondTower
            || To == firstTower && From == secondTower;
    }
}
