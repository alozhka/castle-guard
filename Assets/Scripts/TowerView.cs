using TMPro;
using UnityEngine;

public class TowerView : MonoBehaviour
{
    [SerializeField] private string letter = "A";
    [SerializeField] private TMP_Text label;

    public void SetLetter(string value)
    {
        letter = value;
    }

    private void Awake()
    {
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
