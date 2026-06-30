using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HuXiangLianPian.Accessibility
{
    internal sealed class VisualHintOverlay : MonoBehaviour
    {
        private const float DefaultDuration = 5f;
        private static VisualHintOverlay _instance;

        private Canvas _canvas;
        private TMP_Text _text;
        private Coroutine _hideRoutine;

        public static void Ensure()
        {
            if (_instance != null) return;

            var go = new GameObject("HuXiangLianPianAccessibility_VisualHintOverlay");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<VisualHintOverlay>();
            _instance.Build();
        }

        public static void Show(string message, float duration = DefaultDuration)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            Ensure();
            _instance.ShowInternal(message, duration);
        }

        public static void Hide()
        {
            if (_instance == null) return;
            _instance.HideInternal();
        }

        private void Build()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32760;

            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.76f);

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -24f);
            panelRect.sizeDelta = new Vector2(1160f, 96f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(panel.transform, false);
            _text = textGo.AddComponent<TextMeshProUGUI>();
            _text.fontSize = 28;
            _text.alignment = TextAlignmentOptions.Center;
            _text.enableWordWrapping = true;
            _text.overflowMode = TextOverflowModes.Truncate;
            _text.color = Color.white;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 12f);
            textRect.offsetMax = new Vector2(-24f, -12f);

            _canvas.enabled = false;
        }

        private void ShowInternal(string message, float duration)
        {
            _text.text = message;
            _canvas.enabled = true;

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
            }

            _hideRoutine = StartCoroutine(HideAfter(duration));
        }

        private IEnumerator HideAfter(float duration)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, duration));
            HideInternal();
        }

        private void HideInternal()
        {
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            if (_canvas != null)
            {
                _canvas.enabled = false;
            }
        }
    }
}
