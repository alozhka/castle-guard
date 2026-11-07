using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Castle Guard/Level")]
public class LevelDefinition : ScriptableObject
{
    [Serializable]
    public struct Tower
    {
        public string id;
        public Vector2 position;
    }

    [Serializable]
    public struct Wall
    {
        public string from;
        public string to;
    }

    [SerializeField] private Tower[] towers;
    [SerializeField] private Wall[] walls;
    [SerializeField] private string startingTower;

    public IReadOnlyList<Tower> Towers => towers;
    public IReadOnlyList<Wall> Walls => walls;
    public string StartingTower => startingTower;
}
