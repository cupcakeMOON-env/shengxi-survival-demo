using System.Collections.Generic;
using ShengXi.Core;
using ShengXi.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShengXi.UI
{
    /// <summary>存档位面板这次是拿来干什么的。</summary>
    public enum SlotPanelMode
    {
        /// <summary>开始界面里读档。</summary>
        LoadFromMenu = 0,

        /// <summary>游戏内存档。</summary>
        Save = 1,

        /// <summary>游戏内读档。</summary>
        Load = 2,
    }

    /// <summary>
    /// 开始界面 + 存档位面板。
    /// 首页只有三个按钮：开始新游戏 / 读档 / 退出游戏；
    /// 存档位面板固定三个槽位，按用途分三种模式（开始界面读档、游戏内存档、游戏内读档）。
    /// 显示与隐藏由 MapInitialized 驱动：世界就绪就收起，世界被清空（回开始界面）就显示首页，
    /// 所以「开始新游戏 / 读档 / 回开始界面」都不用各自去操作这个面板。
    /// 纯代码搭建 UI，不依赖场景资产。
    /// </summary>
    public class StartMenuPanel : MonoBehaviour
    {
        private const float PanelWidth = 1920f;
        private const float SlotButtonWidth = 660f;
        private const float SlotButtonHeight = 64f;

        private GameObject _root;
        private GameObject _mainPage;
        private GameObject _slotPage;
        private Text _slotTitleText;
        private Text _hintText;
        private readonly List<Text> _slotLabels = new List<Text>();

        private SlotPanelMode _mode = SlotPanelMode.LoadFromMenu;
        private bool _inGameModal;

        /// <summary>面板是否可见。</summary>
        public bool IsOpen => _root != null && _root.activeSelf;

        /// <summary>当前是否停在存档位页（而不是首页）。</summary>
        public bool IsShowingSlots => _slotPage != null && _slotPage.activeSelf;

        /// <summary>存档位数量（固定三个）。</summary>
        public int SlotCount => _slotLabels.Count;

        public SlotPanelMode Mode => _mode;

        private void Awake()
        {
            BuildUi();
            ShowHomePage();
        }

        private void OnEnable()
        {
            GameEvents.MapInitialized += OnMapInitialized;
        }

        private void OnDisable()
        {
            GameEvents.MapInitialized -= OnMapInitialized;
        }

        /// <summary>世界就绪就收起开始界面，世界被清空就显示首页。</summary>
        private void OnMapInitialized(GridMap map)
        {
            if (map == null)
            {
                ShowHomePage();
            }
            else
            {
                Hide();
            }
        }

        /// <summary>显示首页（开始新游戏 / 读档 / 退出游戏）。</summary>
        public void ShowHomePage()
        {
            _inGameModal = false;
            _mainPage.SetActive(true);
            _slotPage.SetActive(false);
            _root.SetActive(true);
            _hintText.text = string.Empty;
        }

        /// <summary>收起整个面板（回到游戏）。</summary>
        public void Hide()
        {
            SetGamePaused(false);
            _inGameModal = false;
            _root.SetActive(false);
        }

        /// <summary>打开存档位页；游戏内打开时顺便暂停模拟，免得选存档的时候被敌人打死。</summary>
        public void OpenSlots(SlotPanelMode mode)
        {
            _mode = mode;
            _inGameModal = mode != SlotPanelMode.LoadFromMenu;
            _mainPage.SetActive(false);
            _slotPage.SetActive(true);
            _root.SetActive(true);
            _hintText.text = string.Empty;
            SetGamePaused(_inGameModal);
            RefreshSlots();
        }

        /// <summary>开始新游戏。</summary>
        public void StartNewGame()
        {
            if (GameLoop.Instance == null)
            {
                return;
            }

            GameLoop.Instance.NewGame();
        }

        /// <summary>按当前模式对第 index 个槽位存档 / 读档。</summary>
        public void ActivateSlot(int index)
        {
            var loop = GameLoop.Instance;
            var slotId = SaveSlots.IdAt(index);
            if (loop == null || slotId == null)
            {
                return;
            }

            if (_mode == SlotPanelMode.Save)
            {
                loop.SaveGame(slotId);
                RefreshSlots();
                _hintText.text = SaveService.Exists(slotId)
                    ? $"已保存到 {SaveSlots.DisplayName(slotId)}"
                    : "存档失败，请查看日志";
                return;
            }

            var info = SaveService.Inspect(slotId);
            if (!info.Exists)
            {
                _hintText.text = $"{SaveSlots.DisplayName(slotId)} 还是空的";
                return;
            }

            // 读档成功会广播 MapInitialized(非 null)，面板自己就收起来了
            loop.LoadGame(slotId);
        }

        /// <summary>存档位页的「返回」：游戏内回到游戏，开始界面回到首页。</summary>
        public void BackFromSlots()
        {
            if (_inGameModal)
            {
                Hide();
                return;
            }

            ShowHomePage();
        }

        /// <summary>退出游戏。</summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void SetGamePaused(bool paused)
        {
            if (GameLoop.Instance != null)
            {
                GameLoop.Instance.SetPaused(paused);
            }
        }

        private void RefreshSlots()
        {
            var infos = SaveService.InspectAllSlots();
            for (var i = 0; i < _slotLabels.Count && i < infos.Count; i++)
            {
                _slotLabels[i].text = infos[i].Describe();
            }

            _slotTitleText.text = _mode switch
            {
                SlotPanelMode.Save => "保存到哪个存档位？",
                SlotPanelMode.Load => "读取哪个存档？",
                _ => "读取哪个存档？",
            };
        }

        private void BuildUi()
        {
            EnsureEventSystem();
            var canvas = EnsureCanvas();

            _root = new GameObject("StartMenu", typeof(RectTransform), typeof(Image));
            _root.transform.SetParent(canvas.transform, false);
            var rootRt = _root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            var background = _root.GetComponent<Image>();
            // 不透明：菜单背后是空世界或上一局的残影，都不该露出来
            background.color = new Color(0.06f, 0.07f, 0.09f, 1f);
            background.raycastTarget = true;

            var title = CreateText(_root.transform, "Title", "生息演算 Like　生存建造 Demo", 56, new Vector2(0f, 240f));
            title.color = new Color(0.95f, 0.9f, 0.75f);
            var subtitle = CreateText(_root.transform, "Subtitle", "白天建造，夜晚守城，撑过第 7 天", 24, new Vector2(0f, 176f));
            subtitle.color = new Color(0.7f, 0.74f, 0.8f);

            // 首页
            _mainPage = new GameObject("MainPage", typeof(RectTransform));
            _mainPage.transform.SetParent(_root.transform, false);
            Stretch(_mainPage, Vector2.zero, Vector2.one);
            CreateButton(_mainPage.transform, "Btn_开始新游戏", "开始新游戏", new Vector2(0f, 50f), new Vector2(320f, 60f), new Color(0.2f, 0.5f, 0.32f), StartNewGame);
            CreateButton(_mainPage.transform, "Btn_读档", "读档", new Vector2(0f, -30f), new Vector2(320f, 60f), new Color(0.24f, 0.34f, 0.5f), () => OpenSlots(SlotPanelMode.LoadFromMenu));
            CreateButton(_mainPage.transform, "Btn_退出游戏", "退出游戏", new Vector2(0f, -110f), new Vector2(320f, 60f), new Color(0.42f, 0.26f, 0.26f), QuitGame);

            // 存档位页
            _slotPage = new GameObject("SlotPage", typeof(RectTransform));
            _slotPage.transform.SetParent(_root.transform, false);
            Stretch(_slotPage, Vector2.zero, Vector2.one);
            _slotTitleText = CreateText(_slotPage.transform, "SlotTitle", string.Empty, 34, new Vector2(0f, 170f));

            for (var i = 0; i < SaveSlots.Count; i++)
            {
                var index = i;
                var button = CreateButton(
                    _slotPage.transform,
                    $"Btn_Slot{i + 1}",
                    string.Empty,
                    new Vector2(0f, 80f - i * 80f),
                    new Vector2(SlotButtonWidth, SlotButtonHeight),
                    new Color(0.16f, 0.19f, 0.24f),
                    () => ActivateSlot(index));
                _slotLabels.Add(button.GetComponentInChildren<Text>());
            }

            CreateButton(_slotPage.transform, "Btn_返回", "返回", new Vector2(0f, -200f), new Vector2(220f, 52f), new Color(0.28f, 0.3f, 0.35f), BackFromSlots);

            _hintText = CreateText(_root.transform, "Hint", string.Empty, 20, new Vector2(0f, -280f));
            _hintText.color = new Color(0.95f, 0.8f, 0.45f);
        }

        private static void Stretch(GameObject go, Vector2 min, Vector2 max)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(PanelWidth - 120f, 70f);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = content;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position,
            Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
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
            textRt.offsetMin = new Vector2(22f, 0f);
            textRt.offsetMax = new Vector2(-22f, 0f);

            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = label;
            return button;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
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
