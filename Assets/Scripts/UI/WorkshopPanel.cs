using System.Collections.Generic;
using ShengXi.Core;
using ShengXi.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ShengXi.UI
{
    /// <summary>
    /// 工坊面板：选中一座工坊后显示它的产线列表与开关。
    /// 每座工坊独立控制——一个总开关（开工 / 停产）+ 每条配方单独的开关。
    /// 面板不缓存任何玩法数据，显示内容全部现查模拟层，
    /// 并且与结算走同一套判定（ProductionSystem.EvaluateRecipe），所以「缺原料」这类提示不会和实际行为对不上。
    /// 纯代码搭建 UI，不依赖场景资产。
    /// </summary>
    public class WorkshopPanel : MonoBehaviour
    {
        private const float PanelWidth = 470f;
        private const float PanelHeight = 424f;
        private const float RowHeight = 84f;
        private const float Padding = 16f;
        private const float RefreshInterval = 0.2f;

        private static readonly Dictionary<ResourceType, string> ResourceNames = new Dictionary<ResourceType, string>
        {
            { ResourceType.Wood, "木" },
            { ResourceType.Stone, "石" },
            { ResourceType.Food, "食" },
            { ResourceType.Material, "建材" },
            { ResourceType.RepairKit, "修理包" },
        };

        private GameObject _panel;
        private Text _titleText;
        private Text _stateText;
        private Text _ratesText;
        private Text _hintText;
        private Button _producingButton;
        private Text _producingLabel;
        private readonly List<RecipeRow> _rows = new List<RecipeRow>();

        private GridPos _pos;
        private bool _open;
        private float _refreshTimer;

        /// <summary>面板当前是否展开（PlayMode 测试与外部查询用）。</summary>
        public bool IsOpen => _open;

        /// <summary>面板当前绑定的工坊坐标。</summary>
        public GridPos SelectedPosition => _pos;

        /// <summary>面板根对象（隐藏时 GameObject.Find 找不到它，测试与外部查询用这个引用）。</summary>
        public GameObject PanelRoot => _panel;

        private class RecipeRow
        {
            public string RecipeId;
            public string Name;
            public string Formula;
            public Text TitleText;
            public Text StateText;
            public Image ToggleImage;
            public Text ToggleLabel;
        }

        private void Awake()
        {
            BuildUi();
        }

        private void OnEnable()
        {
            GameEvents.BuildingSelected += OnBuildingSelected;
            GameEvents.WorkshopStateChanged += OnWorkshopStateChanged;
            GameEvents.MapInitialized += OnMapInitialized;
            GameEvents.BuildingRemoved += OnBuildingRemoved;
            GameEvents.GameOver += OnGameOver;
        }

        private void OnDisable()
        {
            GameEvents.BuildingSelected -= OnBuildingSelected;
            GameEvents.WorkshopStateChanged -= OnWorkshopStateChanged;
            GameEvents.MapInitialized -= OnMapInitialized;
            GameEvents.BuildingRemoved -= OnBuildingRemoved;
            GameEvents.GameOver -= OnGameOver;
        }

        private void Update()
        {
            if (!_open)
            {
                return;
            }

            // 原料与容量每 tick 都在变，面板按固定间隔重算，
            // 比订阅 ResourceChanged 更省事，也不会被每秒多次的资源事件刷屏
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer < RefreshInterval)
            {
                return;
            }

            _refreshTimer = 0f;
            Refresh();
        }

        /// <summary>切换当前工坊的开工状态（面板按钮与测试共用）。</summary>
        public void ToggleProducing()
        {
            var loop = GameLoop.Instance;
            if (loop == null || !_open)
            {
                return;
            }

            WorkshopService.ToggleProducing(loop.Map, _pos);
            Refresh();
        }

        /// <summary>切换当前工坊某条产线的开关（面板按钮与测试共用）。</summary>
        public void ToggleRecipe(string recipeId)
        {
            var loop = GameLoop.Instance;
            if (loop == null || !_open)
            {
                return;
            }

            WorkshopService.ToggleRecipe(loop.Map, _pos, recipeId);
            Refresh();
        }

        /// <summary>只认工坊：选中别的建筑时把面板收起来。</summary>
        private void OnBuildingSelected(GridPos? pos)
        {
            if (!pos.HasValue)
            {
                Close();
                return;
            }

            var loop = GameLoop.Instance;
            if (loop == null || !WorkshopService.IsWorkshop(loop.Map, pos.Value))
            {
                Close();
                return;
            }

            _pos = pos.Value;
            _open = true;
            _refreshTimer = 0f;
            _panel.SetActive(true);
            Refresh();
        }

        private void OnWorkshopStateChanged(GridPos pos)
        {
            if (_open && pos == _pos)
            {
                Refresh();
            }
        }

        private void OnMapInitialized(GridMap map) => Close();

        private void OnGameOver(RunOutcome outcome, int day) => Close();

        private void OnBuildingRemoved(GridPos pos)
        {
            if (_open && pos == _pos)
            {
                Close();
            }
        }

        private void Close()
        {
            _open = false;
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        /// <summary>按当前模拟状态刷新整个面板；工坊已经不在（被拆 / 换局）时自动收起。</summary>
        private void Refresh()
        {
            var loop = GameLoop.Instance;
            var tile = loop?.Map?.GetTile(_pos);
            if (tile == null || tile.Building != BuildingType.Workshop)
            {
                Close();
                return;
            }

            _titleText.text = $"工坊  ({_pos.X}, {_pos.Y})";

            var producing = tile.ProductionEnabled;
            _stateText.text = producing ? "状态：开工中" : "状态：已停产";
            _stateText.color = producing
                ? new Color(0.45f, 0.9f, 0.5f)
                : new Color(0.95f, 0.62f, 0.3f);
            _producingLabel.text = producing ? "停产" : "开工";

            foreach (var row in _rows)
            {
                var recipe = CraftingCatalog.Find(BuildingType.Workshop, row.RecipeId);
                var enabled = tile.IsRecipeEnabled(row.RecipeId);
                var state = ProductionSystem.EvaluateRecipe(loop.Pool, tile, recipe);

                row.TitleText.text = $"{row.Name}    {row.Formula}";
                row.StateText.text = Describe(state);
                row.StateText.color = ColorFor(state);
                row.ToggleLabel.text = enabled ? "关闭" : "开启";
                row.ToggleImage.color = enabled
                    ? new Color(0.22f, 0.45f, 0.3f)
                    : new Color(0.34f, 0.34f, 0.4f);
            }

            _ratesText.text = BuildRatesText(tile);
            _hintText.text = loop.Cycle.IsNight
                ? "夜晚不生产，回到白天后按这里的配置继续"
                : "工坊只在白天生产；停产或关掉某条产线可以省下原料";
        }

        private static string Describe(RecipeState state) => state switch
        {
            RecipeState.Producing => "生产中",
            RecipeState.WorkshopStopped => "工坊已停产",
            RecipeState.Disabled => "本产线已关闭",
            RecipeState.MissingInputs => "原料不足",
            RecipeState.OutputFull => "容量已满",
            _ => "—",
        };

        private static Color ColorFor(RecipeState state) => state switch
        {
            RecipeState.Producing => new Color(0.45f, 0.9f, 0.5f),
            RecipeState.Disabled => new Color(0.62f, 0.64f, 0.68f),
            RecipeState.WorkshopStopped => new Color(0.95f, 0.62f, 0.3f),
            _ => new Color(0.95f, 0.78f, 0.35f),
        };

        private static string FormatFormula(RecipeDef recipe)
        {
            var parts = new List<string>();
            var inputs = recipe.InputsPerSecond;
            for (var i = 0; i < inputs.Length; i++)
            {
                parts.Add($"{ResourceName(inputs[i].Type)}{inputs[i].AmountPerSecond}");
            }

            return $"{string.Join(" + ", parts)} → {ResourceName(recipe.Output)}{recipe.OutputPerSecond}";
        }

        /// <summary>把「已开启产线」的每秒收支列出来，停掉某条线能省下多少一眼可见。</summary>
        private static string BuildRatesText(Tile tile)
        {
            var rates = WorkshopService.ConfiguredRates(tile);
            var parts = new List<string>();
            for (var i = 0; i < rates.Length; i++)
            {
                if (rates[i] == 0)
                {
                    continue;
                }

                var sign = rates[i] > 0 ? "+" : string.Empty;
                parts.Add($"{ResourceName((ResourceType)i)} {sign}{rates[i]}");
            }

            return parts.Count == 0
                ? "每秒收支：无（工坊已停产）"
                : $"每秒收支：{string.Join("   ", parts)}";
        }

        private static string ResourceName(ResourceType type) =>
            ResourceNames.TryGetValue(type, out var name) ? name : type.ToString();

        private void BuildUi()
        {
            var canvas = EnsureCanvas();

            _panel = new GameObject("WorkshopPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvas.transform, false);
            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            rt.anchoredPosition = new Vector2(-40f, 0f);

            var background = _panel.GetComponent<Image>();
            background.color = new Color(0.09f, 0.11f, 0.14f, 0.93f);
            // 面板要吃掉点击，否则点在按钮上会穿透到地图、把面板关掉
            background.raycastTarget = true;

            _titleText = CreateText(_panel.transform, "Title", string.Empty, 24,
                new Vector2(Padding, -14f), new Vector2(PanelWidth - Padding * 2f, 30f));
            _stateText = CreateText(_panel.transform, "State", string.Empty, 19,
                new Vector2(Padding, -48f), new Vector2(PanelWidth - Padding * 2f, 26f));

            var rowRoot = new GameObject("Rows", typeof(RectTransform));
            rowRoot.transform.SetParent(_panel.transform, false);
            var rowRt = rowRoot.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(0f, 1f);
            rowRt.pivot = new Vector2(0f, 1f);
            rowRt.anchoredPosition = new Vector2(Padding, -80f);
            rowRt.sizeDelta = new Vector2(PanelWidth - Padding * 2f, 0f);

            var recipes = CraftingCatalog.For(BuildingType.Workshop);
            for (var i = 0; i < recipes.Count; i++)
            {
                _rows.Add(CreateRecipeRow(rowRoot.transform, recipes[i], i));
            }

            var ratesY = -80f - RowHeight * recipes.Count - 12f;
            _ratesText = CreateText(_panel.transform, "Rates", string.Empty, 16,
                new Vector2(Padding, ratesY), new Vector2(PanelWidth - Padding * 2f, 46f));
            _hintText = CreateText(_panel.transform, "Hint", string.Empty, 14,
                new Vector2(Padding, ratesY - 48f), new Vector2(PanelWidth - Padding * 2f, 44f));
            _hintText.color = new Color(0.72f, 0.76f, 0.8f);

            _producingButton = CreateButton(_panel.transform, "Btn_Producing", "停产",
                new Vector2(PanelWidth - Padding - 170f, -352f), new Vector2(170f, 46f),
                new Color(0.55f, 0.32f, 0.22f), ToggleProducing);
            _producingLabel = _producingButton.GetComponentInChildren<Text>();

            CreateButton(_panel.transform, "Btn_Close", "关闭",
                new Vector2(Padding, -352f), new Vector2(120f, 46f),
                new Color(0.28f, 0.3f, 0.35f), () => GameEvents.RaiseBuildingSelected(null));

            _panel.SetActive(false);
        }

        private RecipeRow CreateRecipeRow(Transform parent, RecipeDef recipe, int index)
        {
            var y = -RowHeight * index;
            var row = new RecipeRow
            {
                RecipeId = recipe.Id,
                Name = recipe.Name,
                Formula = FormatFormula(recipe),
            };

            row.TitleText = CreateText(parent, $"Title_{recipe.Id}", string.Empty, 19,
                new Vector2(0f, y), new Vector2(280f, 26f));
            row.StateText = CreateText(parent, $"State_{recipe.Id}", string.Empty, 16,
                new Vector2(0f, y - 26f), new Vector2(280f, 24f));

            var button = CreateButton(parent, $"Btn_Recipe_{recipe.Id}", "关闭",
                new Vector2(300f, y - 8f), new Vector2(96f, 40f),
                new Color(0.22f, 0.45f, 0.3f), () => ToggleRecipe(recipe.Id));
            row.ToggleImage = button.GetComponent<Image>();
            row.ToggleLabel = button.GetComponentInChildren<Text>();
            return row;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize,
            Vector2 position, Vector2 size, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position,
            Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            go.GetComponent<Image>().color = color;
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
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = label;
            return button;
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
