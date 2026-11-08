using UnityEngine;

namespace CastleGuard.Level.View
{
    [ExecuteAlways]
    [RequireComponent(typeof(LineRenderer))]
    public class WallView : MonoBehaviour
    {
        [SerializeField] private TowerView from;
        [SerializeField] private TowerView to;
        [SerializeField] private LineRenderer routeLine;

        private LineRenderer _line;

        public void SetTraversed(bool traversed)
        {
            routeLine.enabled = traversed;
        }

        public void SetEndpoints(TowerView start, TowerView end)
        {
            from = start;
            to = end;
            LateUpdate();
        }

        private void OnEnable()
        {
            _line = GetComponent<LineRenderer>();
        }

        private void LateUpdate()
        {
            if (from == null || to == null || from == to)
            {
                _line.positionCount = 0;
                if (routeLine != null)
                {
                    routeLine.positionCount = 0;
                }
                return;
            }

            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.SetPosition(0, from.transform.position);
            _line.SetPosition(1, to.transform.position);

            if (routeLine != null)
            {
                routeLine.useWorldSpace = true;
                routeLine.positionCount = 2;
                routeLine.SetPosition(0, from.transform.position);
                routeLine.SetPosition(1, to.transform.position);
            }
        }
    }
}
