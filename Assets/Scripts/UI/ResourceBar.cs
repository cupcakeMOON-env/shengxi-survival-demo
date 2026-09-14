using ShengXi.Core;
using ShengXi.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ShengXi.UI
{
    /// <summary>
    /// 顶部资源栏：平时只显示一个仓库图标按钮，点击后展开资源面板
    /// （木头/石头/食物/建材/修理包 + 容量、行动点），再次点击收起。
    /// 只订阅 ResourceChanged / ActionPointsChanged 事件刷新文字，
    /// 不轮询、不碰模拟数据；运行时代码搭建 UI，不需要场景或美术资产。
    /// 仓库图标用代码逐像素画进 Texture2D，保持占位美术风格。
    /// 重要：UI 组件必须新建子物体挂在 Canvas 下，绝不能移动 GameRoot——
    /// 否则世界坐标里的网格会被挪进 Canvas 层级，从镜头里消失。
    /// </summary>
    public class ResourceBar : MonoBehaviour
    {
        private const float ButtonSize = 56f;
        private const float Margin = 20f;
        private const float RowHeight = 40f;
        private const float PanelWidth = 300f;

        private Text _woodText;
        private Text _stoneText;
        private Text _foodText;
        private Text _materialText;
        private Text _repairKitText;
        private Text _actionPointText;
        private GameObject _panel;
        private ResourcePool _pool;

        private void Awake()
        {
            var canvas = EnsureCanvas();
            _pool = GameLoop.Instance != null ? GameLoop.Instance.Pool : null;

            // 仓库图标按钮（平时唯一可见的入口）
            var button = CreateToggleButton(canvas.transform);

            // 资源面板：默认隐藏，位于按钮正下方
            _panel = CreatePanel(canvas.transform);
            _woodText = CreateRow(_panel.transform, "WoodRow", 0, new Color(0.62f, 0.44f, 0.26f), "木头");
            _stoneText = CreateRow(_panel.transform, "StoneRow", 1, new Color(0.60f, 0.60f, 0.64f), "石头");
            _foodText = CreateRow(_panel.transform, "FoodRow", 2, new Color(0.85f, 0.42f, 0.35f), "食物");
            _materialText = CreateRow(_panel.transform, "MaterialRow", 3, new Color(0.42f, 0.62f, 0.85f), "建材");
            _repairKitText = CreateRow(_panel.transform, "RepairKitRow", 4, new Color(0.88f, 0.68f, 0.38f), "修理包");
            _actionPointText = CreateRow(_panel.transform, "ActionPointRow", 5, new Color(0.95f, 0.80f, 0.30f), "行动点");

            button.onClick.AddListener(TogglePanel);
            _panel.SetActive(false);

            RefreshAll();
        }

        private void TogglePanel()
        {
            if (_panel == null)
            {
                return;
            }

            var show = !_panel.activeSelf;
            _panel.SetActive(show);
            if (show)
            {
                RefreshAll();
            }
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

            _woodText.text = $"木头  {_pool.GetAmount(ResourceType.Wood)}/{_pool.Capacity}";
            _stoneText.text = $"石头  {_pool.GetAmount(ResourceType.Stone)}/{_pool.Capacity}";
            _foodText.text = $"食物  {_pool.GetAmount(ResourceType.Food)}/{_pool.Capacity}";
            _materialText.text = $"建材  {_pool.GetAmount(ResourceType.Material)}/{_pool.Capacity}";
            _repairKitText.text = $"修理包  {_pool.GetAmount(ResourceType.RepairKit)}/{_pool.Capacity}";
            _actionPointText.text = GameLoop.Instance != null
                ? $"行动点  {GameLoop.Instance.ActionPoints.Current}/{GameLoop.Instance.ActionPoints.MaxPerDay}"
                : "行动点  -";
        }

        private Button CreateToggleButton(Transform canvasTransform)
        {
            var go = new GameObject("WarehouseToggleButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvasTransform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(Margin, -Margin);
            rt.sizeDelta = new Vector2(ButtonSize, ButtonSize);

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = bg;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(8f, 8f);
            iconRt.offsetMax = new Vector2(-8f, -8f);

            var icon = iconGo.GetComponent<Image>();
            icon.sprite = CreateWarehouseIconSprite();
            icon.raycastTarget = false;

            return button;
        }

        private static GameObject CreatePanel(Transform canvasTransform)
        {
            var go = new GameObject("ResourcePanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasTransform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            // 紧贴按钮正下方：顶边 y = -Margin - ButtonSize - 8
            rt.anchoredPosition = new Vector2(Margin, -(Margin + ButtonSize + 8f));
            rt.sizeDelta = new Vector2(PanelWidth, 6f * RowHeight + 24f);

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            bg.raycastTarget = true; // 挡住面板下方的地图点击
            return go;
        }

        private static Text CreateRow(Transform panelTransform, string name, int rowIndex, Color dotColor, string label)
        {
            var rowGo = new GameObject(name, typeof(RectTransform));
            rowGo.transform.SetParent(panelTransform, false);
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(0f, 1f);
            rowRt.pivot = new Vector2(0f, 1f);
            rowRt.anchoredPosition = new Vector2(14f, -(12f + rowIndex * RowHeight));
            rowRt.sizeDelta = new Vector2(PanelWidth - 28f, RowHeight - 4f);

            // 行首色块：用颜色区分资源种类，延续占位美术风格
            var dotGo = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            dotGo.transform.SetParent(rowGo.transform, false);
            var dotRt = dotGo.GetComponent<RectTransform>();
            dotRt.anchorMin = new Vector2(0f, 0.5f);
            dotRt.anchorMax = new Vector2(0f, 0.5f);
            dotRt.pivot = new Vector2(0f, 0.5f);
            dotRt.anchoredPosition = new Vector2(0f, 0f);
            dotRt.sizeDelta = new Vector2(18f, 18f);
            var dot = dotGo.GetComponent<Image>();
            dot.color = dotColor;
            dot.raycastTarget = false;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(rowGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot = new Vector2(0f, 0.5f);
            textRt.offsetMin = new Vector2(28f, 0f);
            textRt.offsetMax = new Vector2(0f, 0f);

            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// 代码绘制 64×64 仓库图标：三角屋顶 + 棕色库身 + 深色库门。
        /// 与 GridView 中仓库建筑的棕色保持同一色系。
        /// </summary>
        private static Sprite CreateWarehouseIconSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            var roof = new Color(0.42f, 0.26f, 0.15f);
            var body = new Color(0.62f, 0.42f, 0.24f);
            var door = new Color(0.25f, 0.16f, 0.10f);
            var clear = new Color(0f, 0f, 0f, 0f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    Color c;
                    if (y >= 46 && y <= 56)
                    {
                        // 屋顶三角：顶点 (32,56)，底边 y=46 跨 x 10..54
                        var halfWidth = Mathf.InverseLerp(56f, 46f, y) * 22f;
                        c = Mathf.Abs(x - 32) <= halfWidth ? roof : clear;
                    }
                    else if (y >= 12 && y < 46 && x >= 14 && x <= 50)
                    {
                        // 库身矩形；库门 (26..38, 12..28)
                        c = y <= 28 && x >= 26 && x <= 38 ? door : body;
                    }
                    else
                    {
                        c = clear;
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
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
