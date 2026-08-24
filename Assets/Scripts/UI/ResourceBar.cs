using ShengXi.Core;
using ShengXi.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ShengXi.UI
{
    /// <summary>
    /// 顶部资源栏：只订阅 ResourceChanged 事件刷新文字，不轮询、不碰模拟数据。
    /// 运行时用代码搭建 Canvas + 三个 Text，不需要任何场景或美术资产。
    /// 重要：UI 组件必须新建子物体挂在 Canvas 下，绝不能移动 GameRoot——
    /// 否则世界坐标里的网格会被挪进 Canvas 层级，从镜头里消失。
    /// </summary>
    public class ResourceBar : MonoBehaviour
    {
        private Text _woodText;
        private Text _stoneText;
        private Text _foodText;
        private Text _actionPointText;
        private ResourcePool _pool;

        private void Awake()
        {
            var canvas = EnsureCanvas();
            _pool = GameLoop.Instance != null ? GameLoop.Instance.Pool : null;

            var barRoot = new GameObject("ResourceBar", typeof(RectTransform));
            barRoot.transform.SetParent(canvas.transform, false);
            var barRt = barRoot.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(0f, 1f);
            barRt.pivot = new Vector2(0f, 1f);
            barRt.anchoredPosition = new Vector2(20f, -12f);
            barRt.sizeDelta = new Vector2(640f, 40f);

            _woodText = CreateLabel(barRoot.transform, "WoodLabel", 0f);
            _stoneText = CreateLabel(barRoot.transform, "StoneLabel", 180f);
            _foodText = CreateLabel(barRoot.transform, "FoodLabel", 360f);
            _actionPointText = CreateLabel(barRoot.transform, "ActionPointLabel", 540f);

            RefreshAll();
        }

        private void OnEnable()
        {
            GameEvents.ResourceChanged += OnResourceChanged;
            GameEvents.ActionPointsChanged += OnActionPointsChanged;
        }

        private void OnDisable()
        {
            GameEvents.ResourceChanged -= OnResourceChanged;
            GameEvents.ActionPointsChanged -= OnActionPointsChanged;
        }

        private void OnResourceChanged(ResourceType type, int amount) => RefreshAll();

        private void OnActionPointsChanged(int current, int max) => RefreshAll();

        private void RefreshAll()
        {
            if (_pool == null)
            {
                return;
            }

            _woodText.text = $"木头: {_pool.GetAmount(ResourceType.Wood)}";
            _stoneText.text = $"石头: {_pool.GetAmount(ResourceType.Stone)}";
            _foodText.text = $"食物: {_pool.GetAmount(ResourceType.Food)}";
            _actionPointText.text = GameLoop.Instance != null
                ? $"行动点: {GameLoop.Instance.ActionPoints.Current}/{GameLoop.Instance.ActionPoints.MaxPerDay}"
                : "行动点: -";
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

        private static Text CreateLabel(Transform parent, string name, float x)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(160f, 36f);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            return text;
        }
    }
}
