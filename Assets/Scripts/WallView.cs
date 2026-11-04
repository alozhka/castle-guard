using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class WallView : MonoBehaviour
{
    [SerializeField] private TowerView from;
    [SerializeField] private TowerView to;

    private LineRenderer _line;

    private void OnEnable()
    {
        _line = GetComponent<LineRenderer>();
    }

    private void LateUpdate()
    {
        if (from == null || to == null || from == to)
        {
            _line.positionCount = 0;
            return;
        }

        _line.useWorldSpace = true;
        _line.positionCount = 2;
        _line.SetPosition(0, from.transform.position);
        _line.SetPosition(1, to.transform.position);
    }
}
