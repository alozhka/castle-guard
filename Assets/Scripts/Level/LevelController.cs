using CastleGuard.Level.Model;
using CastleGuard.Level.View;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CastleGuard.Level
{
    public class LevelController : MonoBehaviour
    {
        [SerializeField] private LevelDefinition[] levels;
        [SerializeField] private TowerView towerPrefab;
        [SerializeField] private WallView wallPrefab;

        [SerializeField] private Button undoButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text levelTitle;

        private readonly Dictionary<string, TowerView> _towers = new();
        private readonly List<WallView> _walls = new();
        private LevelRun _run;
        private int _levelIndex;

        public bool IsComplete => _run != null && _run.IsComplete;
        public bool HasNextLevel => _levelIndex + 1 < levels.Length;

        private void Start()
        {
            LoadLevel();
        }

        public void NextLevel()
        {
            if (!IsComplete || !HasNextLevel)
            {
                return;
            }

            _levelIndex++;
            LoadLevel();
        }

        private void LoadLevel()
        {
            foreach (TowerView tower in _towers.Values)
            {
                tower.Clicked -= HandleTowerClicked;
                tower.gameObject.SetActive(false);
                Destroy(tower.gameObject);
            }

            foreach (WallView wall in _walls)
            {
                wall.gameObject.SetActive(false);
                Destroy(wall.gameObject);
            }

            _towers.Clear();
            _walls.Clear();

            LevelDefinition level = levels[_levelIndex];
            level.ValidateEconomy();
            levelTitle.text = $"Уровень {_levelIndex + 1}";
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
            if (_run != null && !IsComplete && _run.TryUndo())
            {
                RefreshViews();
            }
        }

        public void Restart()
        {
            if (_run == null || IsComplete)
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
            victoryPanel.SetActive(IsComplete);
            nextButton.gameObject.SetActive(HasNextLevel);
            undoButton.interactable = !IsComplete && _run.CanUndo;
            restartButton.interactable = !IsComplete;

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
}
