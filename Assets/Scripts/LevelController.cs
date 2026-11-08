using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LevelController : MonoBehaviour
{
    [SerializeField] private LevelDefinition level;
    [SerializeField] private TowerView towerPrefab;
    [SerializeField] private WallView wallPrefab;

    [SerializeField] private Button undoButton;

    private readonly Dictionary<string, TowerView> _towers = new();
    private readonly List<WallView> _walls = new();
    private LevelRun _run;

    public bool IsComplete => _run != null && _run.IsComplete;

    private void Start()
    {
        List<Wall> connections = level.Walls
            .Select(definition => new Wall(definition.from, definition.to))
            .ToList();
        _run = new LevelRun(level.StartingTower, connections);

        foreach (LevelDefinition.Tower definition in level.Towers)
        {
            TowerView tower = Instantiate(towerPrefab, transform);
            tower.name = $"Tower_{definition.id}";
            tower.transform.localPosition = definition.position;
            tower.SetLetter(definition.id);
            tower.Clicked += HandleTowerClicked;
            _towers.Add(definition.id, tower);
        }

        foreach (LevelDefinition.Wall definition in level.Walls)
        {
            WallView wall = Instantiate(wallPrefab, transform);
            wall.name = $"Wall_{definition.from}_{definition.to}";
            wall.SetEndpoints(_towers[definition.from], _towers[definition.to]);
            _walls.Add(wall);
        }

        RefreshViews();
    }

    public void Undo()
    {
        if (_run != null && _run.TryUndo())
        {
            RefreshViews();
        }
    }

    public void Restart()
    {
        if (_run == null)
        {
            return;
        }

        _run.Restart();
        RefreshViews();
    }

    private void HandleTowerClicked(string towerId)
    {
        if (_run.TryMove(towerId))
        {
            RefreshViews();
        }
    }

    private void RefreshViews()
    {
        undoButton.interactable = _run.CanUndo;

        foreach (KeyValuePair<string, TowerView> tower in _towers)
        {
            tower.Value.SetCurrent(tower.Key == _run.CurrentTower);
        }

        for (int i = 0; i < _walls.Count; i++)
        {
            _walls[i].SetTraversed(_run.IsWallTraversed(i));
        }
    }
}
