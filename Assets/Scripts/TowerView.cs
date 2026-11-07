using TMPro;
using UnityEngine;

public class TowerView : MonoBehaviour
{
    [SerializeField] private string letter = "A";
    [SerializeField] private TMP_Text label;
    [SerializeField] private SpriteRenderer innerCircle;
    private Color defaultColor;

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
