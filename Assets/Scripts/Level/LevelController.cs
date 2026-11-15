using CastleGuard.App;
using CastleGuard.Economy;
using CastleGuard.Level.Model;
using CastleGuard.Level.Generation;
using System.Globalization;
using CastleGuard.Level.View;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CastleGuard.Level
{
    public class LevelController : MonoBehaviour
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
        private LevelAttempt _attempt;
        private LevelDefinition _currentLevel;
        private LevelDefinition _runtimeLevel;
        private HintService _hintService;
        private int _levelIndex;
        private bool _hasFocus;
        private bool _isPaused;
        private bool _clockWasRunning;
        private double _lastTimeSample;

        private bool IsComplete => _attempt is { IsComplete: true };

        private bool IsClockRunning => _attempt is { IsComplete: false }
            && _hasFocus && !_isPaused && Time.timeScale > 0;

        private void OnEnable()
        {
            _hasFocus = Application.isFocused;
            ResetClock();
            GameSave.Wallet.BalanceChanged += HandleBalanceChanged;
            RefreshHintButton();
        }

        private void OnDisable()
        {
            GameSave.Wallet.BalanceChanged -= HandleBalanceChanged;
        }

        private void Start()
        {
            _levelIndex = GameSave.Progress.LevelNumber - 1;
            if (generatedLevel == null)
            {
                _levelIndex = Mathf.Min(_levelIndex, levels.Length - 1);
            }

            LoadLevel();
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
            if (!IsComplete)
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

            _currentLevel = _levelIndex < levels.Length
                ? levels[_levelIndex]
                : CreateGeneratedLevel();
            LevelDefinition level = _currentLevel;
            level.ValidateEconomy();
            levelTitle.text = $"Уровень {_levelIndex + 1}";
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

            StartAttempt();
        }

        private LevelDefinition CreateGeneratedLevel()
        {
            if (_runtimeLevel != null)
            {
                Destroy(_runtimeLevel);
            }

            string levelName = "Уровень " + (_levelIndex + 1).ToString(CultureInfo.InvariantCulture);
            GeneratedLevel data = new LevelGenerator().Generate(levelName);
            _runtimeLevel = LevelDefinition.Create(data, generatedLevel.BaseReward);
            return _runtimeLevel;
        }

        private void OnDestroy()
        {
            if (_runtimeLevel != null)
            {
                Destroy(_runtimeLevel);
            }
        }

        public void Undo()
        {
            if (_attempt != null && _attempt.TryUndo())
            {
                RefreshViews();
            }
        }

        public void Restart()
        {
            if (_attempt == null || IsComplete)
            {
                return;
            }

            StartAttempt();
        }

        private void StartAttempt()
        {
            LevelDefinition level = _currentLevel;
            IEnumerable<Wall> connections = level.Walls
                .Select(definition => new Wall(definition.from, definition.to));
            _attempt = new LevelAttempt(level.StartingTower, connections, level.BaseReward);
            _hintService = new HintService(GameSave.Wallet, _attempt, economySettings.HintPrice);
            hintLabel.text = $"Подсказка  {_hintService.Price}";
            RefreshViews();
            ResetClock();
        }

        public void BuyHint()
        {
            SampleClock();
            if (_hintService?.Purchase().Step is Move step)
            {
                RefreshViews(step.To);
                return;
            }

            RefreshHintButton();
        }

        private void HandleBalanceChanged(int balance)
        {
            RefreshHintButton();
        }

        private void RefreshHintButton()
        {
            hintButton.interactable = _hintService != null && _hintService.CanPurchase;
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
                _attempt.TryAddElapsedTime(now - _lastTimeSample);
            }

            _lastTimeSample = now;
            _clockWasRunning = clockIsRunning;
        }

        private void HandleTowerClicked(string towerId)
        {
            SampleClock();
            if (!_attempt.TryMove(towerId))
            {
                return;
            }

            if (_attempt.Reward.HasValue)
            {
                GameSave.Wallet.Credit(_attempt.Reward.Value.Total);
                GameSave.Progress.CompleteLevel(_levelIndex + 1);

                victoryReward.text = $"+{_attempt.Reward.Value.Total}";
            }

            RefreshViews();
        }

        private void RefreshViews(string hintedTower = null)
        {
            boardRaycaster.enabled = !IsComplete;
            victoryPanel.SetActive(IsComplete);
            nextButton.gameObject.SetActive(value: true);
            undoButton.interactable = _attempt.CanUndo;
            restartButton.interactable = !IsComplete;
            RefreshHintButton();

            foreach (KeyValuePair<string, TowerView> tower in _towers)
            {
                tower.Value.SetState(tower.Key == _attempt.CurrentTower, tower.Key == hintedTower);
            }

            for (int i = 0; i < _walls.Count; i++)
            {
                _walls[i].SetTraversed(_attempt.IsWallTraversed(i));
            }
        }
    }
}
