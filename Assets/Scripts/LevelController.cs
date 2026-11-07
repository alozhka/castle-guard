using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [SerializeField] private LevelDefinition level;
    [SerializeField] private TowerView towerPrefab;
    [SerializeField] private WallView wallPrefab;

    private void Start()
    {
        Dictionary<string, TowerView> towers = new();
        foreach (LevelDefinition.Tower definition in level.Towers)
        {
            TowerView tower = Instantiate(towerPrefab, transform);
            tower.name = $"Tower_{definition.id}";
            tower.transform.localPosition = definition.position;
            tower.SetLetter(definition.id);
            tower.SetCurrent(definition.id == level.StartingTower);
            towers.Add(definition.id, tower);
        }

        foreach (LevelDefinition.Wall definition in level.Walls)
        {
            WallView wall = Instantiate(wallPrefab, transform);
            wall.name = $"Wall_{definition.from}_{definition.to}";
            wall.SetEndpoints(towers[definition.from], towers[definition.to]);
        }
    }
}
