using ShengXi.Core;
using ShengXi.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ShengXi.UI
{
    /// <summary>
    /// 顶部状态横幅：第 N 天 · 白天/夜晚 · 据点 HP · 敌人数量；
    /// 游戏结束时显示胜负结果。
    /// </summary>
    public class DayBanner : MonoBehaviour
    {
        private Text _statusText;
        private Text _resultText;

        private void Awake()
        {
            var canvas = EnsureCanvas();

            var root = new GameObject("DayBanner", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -12f);
            rt.sizeDelta = new Vector2(900f, 40f);

            _statusText = CreateText(root.transform, "StatusText", 28);
            _resultText = CreateText(root.transform, "ResultText", 56);
            var resultRt = _resultText.rectTransform;
            resultRt.anchorMin = new Vector2(0.5f, 0.5f);
            resultRt.anchorMax = new Vector2(0.5f, 0.5f);
            resultRt.anchoredPosition = Vector2.zero;
            resultRt.sizeDelta = new Vector2(900f, 120f);
            _resultText.gameObject.SetActive(false);

            Refresh();
        }

        private void OnEnable()
        {
            GameEvents.PhaseChanged += OnChanged;
            GameEvents.DayChanged += OnChanged;
            GameEvents.BaseHpChanged += OnChanged;
            GameEvents.EnemyCountChanged += OnChanged;
            GameEvents.GameOver += OnGameOver;
        }

        private void OnDisable()
        {
            GameEvents.PhaseChanged -= OnChanged;
            GameEvents.DayChanged -= OnChanged;
            GameEvents.BaseHpChanged -= OnChanged;
            GameEvents.EnemyCountChanged -= OnChanged;
            GameEvents.GameOver -= OnGameOver;
        }

        private void OnChanged(int unused) => Refresh();

        private void OnChanged(int unusedA, int unusedB) => Refresh();

        private void OnChanged(DayPhase phase) => Refresh();

        private void OnGameOver(bool victory, int day)
        {
            Refresh();
            _resultText.gameObject.SetActive(true);
            _resultText.text = victory
                ? $"胜利！撑过了第 {day} 天"
                : "失败！据点被摧毁";
            _resultText.color = victory ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.95f, 0.35f, 0.35f);
        }

        private void Refresh()
        {
            var loop = GameLoop.Instance;
            if (loop == null || _statusText == null)
            {
                return;
            }

            var phase = loop.Cycle.IsDay ? "白天" : "夜晚";
            var baseInfo = loop.Base != null
                ? $"据点 {loop.Base.CurrentHp}/{loop.Base.MaxHp}"
                : "据点 未放置";
            var hint = loop.IsChoosingBase ? " · 左键点击地图放置据点" : "";
            _statusText.text = $"第 {loop.Cycle.Day} 天 · {phase} · {baseInfo} · 敌人 {loop.Enemies.Count}{hint}";
        }

        private static Text CreateText(Transform parent, string name, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Canvas EnsureCanvas()
        {
            var existing = Object.FindAnyObjectByType<Canvas>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return canvas;
        }
    }
}
