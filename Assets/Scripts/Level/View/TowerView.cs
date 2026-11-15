using System;
using CastleGuard.Level.ViewModel;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CastleGuard.Level.View
{
    public class TowerView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private string letter = "A";
        [SerializeField] private TMP_Text label;
        [SerializeField] private SpriteRenderer innerCircle;
        [SerializeField] private Color hintColor = new(0.32f, 0.9f, 1f);
        private Color _defaultColor;

        private TowerViewModel _viewModel;
        private Action _sampleClock;

        public void Initialize(TowerViewModel viewModel, Action sampleClock)
        {
            Unsubscribe();
            _viewModel = viewModel;
            _sampleClock = sampleClock;
            letter = _viewModel.Id;
            RefreshLabel();
            if (isActiveAndEnabled)
            {
                _viewModel.StateChanged += Render;
                Render();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && _viewModel != null)
            {
                _sampleClock?.Invoke();
                _viewModel.Click();
            }
        }

        private void Render()
        {
            Color color = _viewModel.IsCurrent ? new Color(1f, 0.58f, 0.2f) : _defaultColor;
            innerCircle.color = _viewModel.IsHinted ? hintColor : color;
        }

        private void OnEnable()
        {
            if (_viewModel != null)
            {
                _viewModel.StateChanged += Render;
                Render();
            }
        }

        private void OnDisable() => Unsubscribe();

        private void Unsubscribe()
        {
            if (_viewModel != null)
            {
                _viewModel.StateChanged -= Render;
            }
        }

        private void Awake()
        {
            _defaultColor = innerCircle.color;
            RefreshLabel();
        }

        private void OnValidate()
        {
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (label != null)
            {
                label.text = letter;
            }
        }
    }
}
