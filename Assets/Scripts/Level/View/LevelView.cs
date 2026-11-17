using System.Collections.Generic;
using CastleGuard.App;
using CastleGuard.Level.Model;
using CastleGuard.Level.Model.Definition;
using CastleGuard.Level.ViewModel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CastleGuard.Level.View
{
    public class LevelView : MonoBehaviour
    {
        [SerializeField] private TowerView towerPrefab;
        [SerializeField] private WallView wallPrefab;
        [SerializeField] private Physics2DRaycaster boardRaycaster;

        [SerializeField] private Button undoButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Text hintLabel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text levelTitle;
        [SerializeField] private Text victoryReward;

        private readonly Dictionary<string, TowerView> _towers = new();
        private readonly List<WallView> _walls = new();
        private GameModel _game;
        private LevelModel _model;
        private LevelViewModel _viewModel;
        private LevelDefinition _displayedLevel;
        private bool _hasFocus;
        private bool _isPaused;
        private bool _clockWasRunning;
        private double _lastTimeSample;

        private bool IsClockRunning => _viewModel is { IsComplete: false }
            && _hasFocus && !_isPaused && Time.timeScale > 0;

        private void Awake()
        {
            _game = GameSession.Model;
            _model = _game.StartLevel();
            Initialize(new LevelViewModel(_model, () => _game.CurrentLevel));
        }

        private void OnDestroy()
        {
            _game?.EndLevel(_model);
        }

        public void Initialize(LevelViewModel viewModel)
        {
            if (_viewModel != null)
            {
                _viewModel.StateChanged -= Render;
            }

            _viewModel = viewModel;
            _displayedLevel = null;
            if (isActiveAndEnabled)
            {
                OnEnable();
            }
        }

        private void OnEnable()
        {
            if (_viewModel == null)
            {
                return;
            }

            _hasFocus = Application.isFocused;
            _viewModel.StateChanged -= Render;
            _viewModel.StateChanged += Render;
            Render();
            ResetClock();
        }

        private void OnDisable()
        {
            if (_viewModel != null)
            {
                _viewModel.StateChanged -= Render;
            }
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

            foreach (Tower definition in level.Towers)
            {
                TowerView tower = Instantiate(towerPrefab, transform);
                tower.name = $"Tower_{definition.Id}";
                tower.transform.localPosition = definition.Position;
                tower.Initialize(_viewModel.CreateTower(definition.Id), SampleClock);
                _towers.Add(definition.Id, tower);
            }

            foreach (Wall definition in level.Walls)
            {
                WallView wall = Instantiate(wallPrefab, transform);
                wall.name = $"Wall_{definition.From}_{definition.To}";
                wall.SetEndpoints(_towers[definition.From], _towers[definition.To]);
                _walls.Add(wall);
            }

            _displayedLevel = level;
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
            if (_displayedLevel != _viewModel.CurrentLevel)
            {
                BuildBoard(_viewModel.CurrentLevel);
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
