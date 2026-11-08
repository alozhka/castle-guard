using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [SerializeField] private LevelDefinition level;
    [SerializeField] private TowerView towerPrefab;
    [SerializeField] private WallView wallPrefab;

    private readonly Dictionary<string, TowerView> towers = new();
    private readonly List<WallView> walls = new();
    private LevelRun run;

    public bool IsComplete => run != null && run.IsComplete;

    private void Start()
    {
        List<(string From, string To)> connections = new();
        foreach (LevelDefinition.Wall definition in level.Walls)
        {
            connections.Add((definition.from, definition.to));
        }
        run = new(level.StartingTower, connections);

        foreach (LevelDefinition.Tower definition in level.Towers)
        {
            TowerView tower = Instantiate(towerPrefab, transform);
            tower.name = $"Tower_{definition.id}";
            tower.transform.localPosition = definition.position;
            tower.SetLetter(definition.id);
            tower.Clicked += HandleTowerClicked;
            towers.Add(definition.id, tower);
        }

        foreach (LevelDefinition.Wall definition in level.Walls)
        {
            WallView wall = Instantiate(wallPrefab, transform);
            wall.name = $"Wall_{definition.from}_{definition.to}";
            wall.SetEndpoints(towers[definition.from], towers[definition.to]);
            walls.Add(wall);
        }

        RefreshViews();
    }

    private void HandleTowerClicked(string towerId)
    {
        if (run.TryMove(towerId))
        {
            RefreshViews();
        }
    }

    private void RefreshViews()
    {
        foreach (KeyValuePair<string, TowerView> tower in towers)
        {
            tower.Value.SetCurrent(tower.Key == run.CurrentTower);
        }

        for (int i = 0; i < walls.Count; i++)
        {
            walls[i].SetTraversed(run.IsWallTraversed(i));
        }
    }
}
