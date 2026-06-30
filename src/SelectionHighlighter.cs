using UnityEngine;
using UnityEngine.UI;

namespace HuXiangLianPian.Accessibility
{
    internal static class SelectionHighlighter
    {
        private static GameObject _current;
        private static Outline _outline;
        private static bool _createdOutline;
        private static bool _previousEnabled;
        private static Color _previousColor;
        private static Vector2 _previousDistance;

        public static void Select(GameObject selected)
        {
            if (_current == selected) return;

            Clear();

            if (selected == null) return;

            var selectable = selected.GetComponent<Selectable>();
            if (selectable == null) return;

            Graphic graphic = selectable.targetGraphic;
            if (graphic == null)
            {
                graphic = selected.GetComponentInChildren<Graphic>(true);
            }

            if (graphic == null) return;

            _current = selected;
            _outline = graphic.GetComponent<Outline>();
            _createdOutline = _outline == null;
            if (_createdOutline)
            {
                _outline = graphic.gameObject.AddComponent<Outline>();
            }
            else
            {
                _previousEnabled = _outline.enabled;
                _previousColor = _outline.effectColor;
                _previousDistance = _outline.effectDistance;
            }

            _outline.effectColor = new Color(1f, 0.92f, 0.18f, 1f);
            _outline.effectDistance = new Vector2(5f, -5f);
            _outline.useGraphicAlpha = false;
            _outline.enabled = true;
        }

        public static void Clear()
        {
            if (_outline != null)
            {
                if (_createdOutline)
                {
                    Object.Destroy(_outline);
                }
                else
                {
                    _outline.effectColor = _previousColor;
                    _outline.effectDistance = _previousDistance;
                    _outline.enabled = _previousEnabled;
                }
            }

            _current = null;
            _outline = null;
            _createdOutline = false;
        }
    }
}
