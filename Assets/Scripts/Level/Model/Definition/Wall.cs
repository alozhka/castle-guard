using System;
using UnityEngine;

namespace CastleGuard.Level.Model.Definition;

[Serializable]
public struct Wall
{
    [SerializeField] private string from;
    [SerializeField] private string to;

    public string From => from;
    public string To => to;

    public Wall(string from, string to)
    {
        this.from = from;
        this.to = to;
    }

    public bool Connects(string firstTower, string secondTower)
    {
        return From == firstTower && To == secondTower
            || To == firstTower && From == secondTower;
    }
}
