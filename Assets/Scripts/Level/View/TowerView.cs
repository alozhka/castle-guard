using System;
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

        public event Action<string> Clicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Clicked?.Invoke(letter);
            }
        }

        public void SetState(bool current, bool hinted)
        {
            Color color = current ? new Color(1f, 0.58f, 0.2f) : _defaultColor;
            innerCircle.color = hinted ? hintColor : color;
        }

        public void SetLetter(string value)
        {
            letter = value;
            RefreshLabel();
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
