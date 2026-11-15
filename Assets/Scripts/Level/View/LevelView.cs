using CastleGuard.App;
using CastleGuard.Economy;
using System.Collections.Generic;
using CastleGuard.Level.Model;
using CastleGuard.Level.ViewModel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CastleGuard.Level.View
{
    public class LevelView : MonoBehaviour
    {
        [SerializeField] private LevelDefinition[] levels;
        [SerializeField] private LevelDefinition generatedLevel;
        [SerializeField] private TowerView towerPrefab;
        [SerializeField] private WallView wallPrefab;
        [SerializeField] private Physics2DRaycaster boardRaycaster;

        [SerializeField] private Button undoButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Text hintLabel;
        [SerializeField] private EconomySettings economySettings;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text levelTitle;
        [SerializeField] private Text victoryReward;

        private readonly Dictionary<string, TowerView> _towers = new();
        private readonly List<WallView> _walls = new();
        private LevelModel _model;
        private LevelViewModel _viewModel;
        private LevelSource _source;
        private LevelDefinition _displayedLevel;
        private bool _hasFocus;
        private bool _isPaused;
        private bool _clockWasRunning;
        private double _lastTimeSample;

        private bool IsClockRunning => _viewModel is { IsComplete: false }
            && _hasFocus && !_isPaused && Time.timeScale > 0;

        private void Awake()
        {
            _source = new LevelSource(levels, generatedLevel);
            int initialIndex = _source.GetInitialIndex(GameSave.Progress.LevelNumber - 1);
            _model = new LevelModel(
                _source.CreateAttempt, GameSave.Wallet, GameSave.Progress.CompleteLevel,
                economySettings.HintPrice, initialIndex);
            _viewModel = new LevelViewModel(_model);
        }

        private void OnEnable()
        {
            _hasFocus = Application.isFocused;
            _viewModel.StateChanged += Render;
            Render();
            ResetClock();
        }

        private void OnDisable()
        {
            _viewModel.StateChanged -= Render;
        }

        private void Update()
        {
            SampleClock();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            SampleClock();
            _hasFocus = hasFocus;
            ResetClock();
        }

        private void OnApplicationPause(bool isPaused)
        {
            SampleClock();
            _isPaused = isPaused;
            ResetClock();
        }

        public void NextLevel()
        {
            _viewModel.NextLevel();
            ResetClock();
        }

        private void BuildBoard(LevelDefinition level)
        {
            foreach (TowerView tower in _towers.Values)
            {
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

            foreach (LevelDefinition.Tower definition in level.Towers)
            {
                TowerView tower = Instantiate(towerPrefab, transform);
                tower.name = $"Tower_{definition.id}";
                tower.transform.localPosition = definition.position;
                tower.Initialize(_viewModel.CreateTower(definition.id), SampleClock);
                _towers.Add(definition.id, tower);
            }

            foreach (LevelDefinition.Wall definition in level.Walls)
            {
                WallView wall = Instantiate(wallPrefab, transform);
                wall.name = $"Wall_{definition.from}_{definition.to}";
                wall.SetEndpoints(_towers[definition.from], _towers[definition.to]);
                _walls.Add(wall);
            }

            _displayedLevel = level;
        }

        private void OnDestroy()
        {
            _model?.Dispose();
            _source?.Dispose();
        }

        public void Undo() => _viewModel.Undo();

        public void Restart()
        {
            _viewModel.Restart();
            ResetClock();
        }

        public void BuyHint()
        {
            SampleClock();
            _viewModel.BuyHint();
        }

        private void ResetClock()
        {
            _lastTimeSample = Time.realtimeSinceStartupAsDouble;
            _clockWasRunning = IsClockRunning;
        }

        private void SampleClock()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            bool clockIsRunning = IsClockRunning;
            if (_clockWasRunning && clockIsRunning)
            {
                _viewModel.AddElapsedTime(now - _lastTimeSample);
            }

            _lastTimeSample = now;
            _clockWasRunning = clockIsRunning;
        }

        private void Render()
        {
            if (_displayedLevel != _source.Current)
            {
                BuildBoard(_source.Current);
            }

            levelTitle.text = _viewModel.LevelTitle;
            hintLabel.text = _viewModel.HintText;
            victoryReward.text = _viewModel.RewardText;
            boardRaycaster.enabled = !_viewModel.IsComplete;
            victoryPanel.SetActive(_viewModel.IsComplete);
            nextButton.gameObject.SetActive(value: true);
            undoButton.interactable = _viewModel.CanUndo;
            restartButton.interactable = _viewModel.CanRestart;
            hintButton.interactable = _viewModel.CanBuyHint;

            for (int i = 0; i < _walls.Count; i++)
            {
                _walls[i].SetTraversed(_viewModel.IsWallTraversed(i));
            }
        }
    }
}
