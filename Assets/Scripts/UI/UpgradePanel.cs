using ShengXi.Core;
using ShengXi.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ShengXi.UI
{
    /// <summary>
    /// 建筑升级面板：点箭塔 / 围墙后弹出，把「升级要什么、升完变成什么、现在还差什么」一次说清，
    /// 玩家确认后才真正升级（取代了原来"先点升级模式、再点目标"的两步操作）。
    /// 数据全部现查模拟层，判定与 UpgradeService 共用一套（UpgradeState），
    /// 所以面板上说"建材不足"就一定是升级时真正被拦下的原因。
    /// 纯代码搭建 UI，不依赖场景资产。
    /// </summary>
    public class UpgradePanel : MonoBehaviour
    {
        private const float PanelWidth = 470f;
        private const float PanelHeight = 400f;
        private const float Padding = 18f;
        private const float RefreshInterval = 0.2f;

        private GameObject _panel;
        private Text _titleText;
        private Text _levelText;
        private Text _costText;
        private Text _effectText;
        private Text _stateText;
        private Text _hintText;
        private Image _confirmImage;

        private GridPos _pos;
        private bool _open;
        private float _refreshTimer;

        public bool IsOpen => _open;
        public GridPos SelectedPosition => _pos;
        public GameObject PanelRoot => _panel;

        /// <summary>升级消耗文字（PlayMode 测试用）。</summary>
        public string CostText => _costText != null ? _costText.text : string.Empty;

        /// <summary>升级效果文字（PlayMode 测试用）。</summary>
        public string EffectText => _effectText != null ? _effectText.text : string.Empty;

        /// <summary>当前状态文字（PlayMode 测试用）。</summary>
        public string StateText => _stateText != null ? _stateText.text : string.Empty;

        private void Awake()
        {
            BuildUi();
        }

        private void OnEnable()
        {
            GameEvents.BuildingSelected += OnBuildingSelected;
            GameEvents.BuildingUpgraded += OnBuildingUpgraded;
            GameEvents.BuildingRemoved += OnBuildingRemoved;
            GameEvents.MapInitialized += OnMapInitialized;
            GameEvents.GameOver += OnGameOver;
        }

        private void OnDisable()
        {
            GameEvents.BuildingSelected -= OnBuildingSelected;
            GameEvents.BuildingUpgraded -= OnBuildingUpgraded;
            GameEvents.BuildingRemoved -= OnBuildingRemoved;
            GameEvents.MapInitialized -= OnMapInitialized;
            GameEvents.GameOver -= OnGameOver;
        }

        private void Update()
        {
            if (!_open)
            {
                return;
            }

            // 建材与行动点随时在变，按固定间隔重算，保证"还差多少"始终是当前值
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer < RefreshInterval)
            {
                return;
            }

            _refreshTimer = 0f;
            Refresh();
        }

        /// <summary>确认升级（面板按钮与测试共用）。</summary>
        public bool ConfirmUpgrade()
        {
            var loop = GameLoop.Instance;
            if (loop == null || !_open)
            {
                return false;
            }

            var upgraded = UpgradeService.TryUpgrade(loop.Map, loop.Pool, loop.ActionPoints, _pos);
            if (upgraded)
            {
                Debug.Log($"[UpgradePanel] 升级成功 grid={_pos}");
            }

            Refresh();
            return upgraded;
        }

        private void OnBuildingSelected(GridPos? pos)
        {
            if (!pos.HasValue)
            {
                Close();
                return;
            }

            var loop = GameLoop.Instance;
            // 只认可升级建筑：选中工坊或空地时把自己收起来（工坊有它自己的产线面板）
            if (loop == null || !UpgradeService.IsUpgradable(loop.Map, pos.Value))
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

        private void OnBuildingUpgraded(GridPos pos, BuildingType building, int level)
        {
            if (_open && pos == _pos)
            {
                Refresh();
            }
        }

        private void OnBuildingRemoved(GridPos pos)
        {
            if (_open && pos == _pos)
            {
                Close();
            }
        }

        private void OnMapInitialized(GridMap map) => Close();

        private void OnGameOver(RunOutcome outcome, int day) => Close();

        private void Close()
        {
            _open = false;
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void Refresh()
        {
            var loop = GameLoop.Instance;
            var tile = loop?.Map?.GetTile(_pos);
            if (tile == null || !UpgradeService.IsUpgradable(loop.Map, _pos))
            {
                Close();
                return;
            }

            var def = BuildingCatalog.Get(tile.Building);
            var level = tile.BuildingLevel;
            var state = UpgradeService.Evaluate(loop.Map, loop.Pool, loop.ActionPoints, _pos);
            var materialHave = loop.Pool.GetAmount(ResourceType.Material);
            var apHave = loop.ActionPoints.Current;

            _titleText.text = $"{def.Name}　({_pos.X}, {_pos.Y})";
            _levelText.text = $"当前等级　{level} / {UpgradeService.MaxLevel}";

            // 消耗：把"需要多少"和"我手上有多少"并排写，缺多少一眼看到
            _costText.text =
                $"升级消耗\n" +
                $"建材 {def.UpgradeMaterialCost}　（现有 {materialHave}）\n" +
                $"行动点 {UpgradeService.ActionPointCost}　（现有 {apHave}）";

            _effectText.text = BuildEffectText(def, level, state);
            _stateText.text = Describe(state);
            _stateText.color = ColorFor(state);

            _confirmImage.color = state == UpgradeState.Ok
                ? new Color(0.2f, 0.52f, 0.32f)
                : new Color(0.32f, 0.33f, 0.38f);

            _hintText.text = state == UpgradeState.MaxLevel
                ? $"已是最高等级（{UpgradeService.MaxLevel} 级）"
                : "会消耗 1 个行动点；升级同时把建筑整修到新等级满血";
        }

        /// <summary>只列出这一级真正会变的属性，避免面板上堆一堆没动的数字。</summary>
        private static string BuildEffectText(BuildingDef def, int level, UpgradeState state)
        {
            if (state == UpgradeState.MaxLevel)
            {
                return $"满级属性\n伤害 {BuildingStats.Damage(def, level)}　血量 {BuildingStats.MaxHp(def, level)}";
            }

            var next = level + 1;
            var lines = "升级后";
            if (def.DamagePerLevel > 0)
            {
                lines += $"\n伤害 {BuildingStats.Damage(def, level)} → {BuildingStats.Damage(def, next)}";
            }

            if (def.HpPerLevel > 0)
            {
                lines += $"\n血量 {BuildingStats.MaxHp(def, level)} → {BuildingStats.MaxHp(def, next)}";
            }

            return lines;
        }

        private static string Describe(UpgradeState state) => state switch
        {
            UpgradeState.Ok => "状态：可以升级",
            UpgradeState.MaxLevel => "状态：已满级",
            UpgradeState.MissingMaterial => "状态：建材不足",
            UpgradeState.MissingActionPoint => "状态：行动点不足",
            _ => "状态：该建筑不可升级",
        };

        private static Color ColorFor(UpgradeState state) => state switch
        {
            UpgradeState.Ok => new Color(0.45f, 0.9f, 0.5f),
            UpgradeState.MaxLevel => new Color(0.62f, 0.64f, 0.68f),
            _ => new Color(0.95f, 0.78f, 0.35f),
        };

        private void BuildUi()
        {
            var canvas = EnsureCanvas();

            _panel = new GameObject("UpgradePanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvas.transform, false);
            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            rt.anchoredPosition = new Vector2(-40f, 0f);

            var background = _panel.GetComponent<Image>();
            background.color = new Color(0.09f, 0.11f, 0.14f, 0.93f);
            background.raycastTarget = true;

            var y = -14f;
            _titleText = CreateText(_panel.transform, "Title", string.Empty, 24, y);
            y -= 36f;
            _levelText = CreateText(_panel.transform, "Level", string.Empty, 19, y);
            y -= 40f;
            _costText = CreateText(_panel.transform, "Cost", string.Empty, 19, y, 92f);
            y -= 100f;
            _effectText = CreateText(_panel.transform, "Effect", string.Empty, 19, y, 78f);
            y -= 86f;
            _stateText = CreateText(_panel.transform, "State", string.Empty, 20, y);
            y -= 34f;
            _hintText = CreateText(_panel.transform, "Hint", string.Empty, 14, y, 40f);
            _hintText.color = new Color(0.72f, 0.76f, 0.8f);

            CreateButton(_panel.transform, "Btn_升级关闭", "关闭",
                new Vector2(Padding, -PanelHeight + 60f), new Vector2(130f, 46f),
                new Color(0.28f, 0.3f, 0.35f), () => GameEvents.RaiseBuildingSelected(null));
            var confirm = CreateButton(_panel.transform, "Btn_确认升级", "确认升级",
                new Vector2(PanelWidth - Padding - 180f, -PanelHeight + 60f), new Vector2(180f, 46f),
                new Color(0.32f, 0.33f, 0.38f), () => ConfirmUpgrade());
            _confirmImage = confirm.GetComponent<Image>();

            _panel.SetActive(false);
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, float y, float height = 28f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(Padding, y);
            rt.sizeDelta = new Vector2(PanelWidth - Padding * 2f, height);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.UpperLeft;
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
