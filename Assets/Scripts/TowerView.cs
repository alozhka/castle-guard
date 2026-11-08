using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TowerView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string letter = "A";
    [SerializeField] private TMP_Text label;
    [SerializeField] private SpriteRenderer innerCircle;
    private Color defaultColor;

    public event Action<string> Clicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Clicked?.Invoke(letter);
        }
    }

    public void SetCurrent(bool current)
    {
        innerCircle.color = current ? new Color(1f, 0.58f, 0.2f) : defaultColor;
    }

    public void SetLetter(string value)
    {
        letter = value;
        RefreshLabel();
    }

    private void Awake()
    {
        defaultColor = innerCircle.color;
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
