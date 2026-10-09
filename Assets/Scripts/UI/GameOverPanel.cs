using ShengXi.Core;
using ShengXi.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ShengXi.UI
{
    /// <summary>
    /// 结算面板：游戏结束时显示胜负与天数，提供「重新开始」（重载场景）。
    /// </summary>
    public class GameOverPanel : MonoBehaviour
    {
        private GameObject _panel;
        private Text _resultText;
        private Text _detailText;
        private Text _statsText;

        private void Awake()
        {
            var canvas = EnsureCanvas();

            _panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvas.transform, false);
            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var image = _panel.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.74f);
            image.raycastTarget = true;

            _resultText = CreateText(_panel.transform, "ResultText", 64, new Vector2(0f, 230f));
            _detailText = CreateText(_panel.transform, "DetailText", 34, new Vector2(0f, 150f));
            _statsText = CreateText(_panel.transform, "StatsText", 26, new Vector2(0f, -30f));
            _statsText.rectTransform.sizeDelta = new Vector2(1000f, 220f);
            CreateButton(_panel.transform, new Vector2(-125f, -250f), "再来一局", () =>
            {
                if (GameLoop.Instance != null)
                {
                    // 直接原地重开，不用重载场景：世界由 NewGame 重建，视图靠 MapInitialized 重建
                    GameLoop.Instance.NewGame();
                }
            });
            CreateButton(_panel.transform, new Vector2(125f, -250f), "返回开始界面", () =>
            {
                if (GameLoop.Instance != null)
                {
                    GameLoop.Instance.ReturnToMenu();
                }
            });

            _panel.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.GameOver += OnGameOver;
            GameEvents.MapInitialized += OnMapInitialized;
        }

        private void OnDisable()
        {
            GameEvents.GameOver -= OnGameOver;
            GameEvents.MapInitialized -= OnMapInitialized;
        }

        private void OnMapInitialized(GridMap map) => _panel.SetActive(false);

        private void OnGameOver(RunOutcome outcome, int day)
        {
            _resultText.text = GameOverText.Headline(outcome);
            _resultText.color = GameOverText.ColorFor(outcome);
            _detailText.text = GameOverText.Summary(outcome, day);
            _statsText.text = GameOverText.Stats(
                GameLoop.Instance != null ? GameLoop.Instance.BuildSummary(outcome) : null);
            _panel.SetActive(true);
        }

        /// <summary>结算详情文字（PlayMode 测试用）。</summary>
        public string DetailText => _detailText != null ? _detailText.text : string.Empty;

        /// <summary>结算统计文字（PlayMode 测试用）。</summary>
        public string StatsText => _statsText != null ? _statsText.text : string.Empty;

        /// <summary>结算页当前是否可见（PlayMode 测试用）。</summary>
        public bool IsVisible => _panel != null && _panel.activeSelf;

        private static Text CreateText(Transform parent, string name, int fontSize, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(900f, 90f);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateButton(Transform parent, Vector2 position, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(220f, 56f);

            go.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.3f, 1f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.onClick.AddListener(onClick);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = label;
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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            return canvas;
        }
    }
}
