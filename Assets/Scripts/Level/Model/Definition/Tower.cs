using System;
using UnityEngine;

namespace CastleGuard.Level.Model.Definition;

[Serializable]
public struct Tower
{
    [SerializeField] private string id;
    [SerializeField] private Vector2 position;

    public string Id => id;
    public Vector2 Position => position;

    public Tower(string id, Vector2 position)
    {
        this.id = id;
        this.position = position;
    }
}
