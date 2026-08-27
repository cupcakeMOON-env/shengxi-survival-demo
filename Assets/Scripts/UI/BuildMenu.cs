using ShengXi.Core;
using ShengXi.Simulation;
using ShengXi.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShengXi.UI
{
    /// <summary>
    /// 底部建造菜单：每个建筑一个按钮（显示名称+造价），点击进入/退出建造模式；
    /// 「结束回合」重置行动点（M3 会接上真正的日夜切换）。
    /// 纯代码搭建 UI，不依赖场景资产。
    /// </summary>
    public class BuildMenu : MonoBehaviour
    {
        private BuildModeController _buildMode;

        private void Awake()
        {
            _buildMode = Object.FindAnyObjectByType<BuildModeController>();
            EnsureEventSystem();

            var canvas = EnsureCanvas();
            var barRoot = new GameObject("BuildMenu", typeof(RectTransform));
            barRoot.transform.SetParent(canvas.transform, false);
            var rt = barRoot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 12f);

            var x = 0f;

            foreach (var def in BuildingCatalog.All)
            {
                x = CreateButton(barRoot.transform, x, $"{def.Name}\n{FormatCost(def)}", () => _buildMode.SetActive(def));
            }

            CreateButton(barRoot.transform, x, "拆除", () => _buildMode.SetDemolishActive());

            CreateButton(barRoot.transform, x, "结束白天", () =>
            {
                if (GameLoop.Instance != null)
                {
                    GameLoop.Instance.EndDay();
                }
            });

            CreateButton(barRoot.transform, x, "存档", () =>
            {
                if (GameLoop.Instance != null)
                {
                    GameLoop.Instance.SaveGame();
                }
            });

            CreateButton(barRoot.transform, x, "读档", () =>
            {
                if (GameLoop.Instance != null)
                {
                    GameLoop.Instance.LoadGame();
                }
            });
        }

        private static string FormatCost(BuildingDef def)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (def.WoodCost > 0) parts.Add($"木{def.WoodCost}");
            if (def.StoneCost > 0) parts.Add($"石{def.StoneCost}");
            if (def.FoodCost > 0) parts.Add($"食{def.FoodCost}");
            parts.Add($"点{def.ActionPointCost}");
            return string.Join(" ", parts);
        }

        private static float CreateButton(Transform parent, float x, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{label.Split('\n')[0]}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(130f, 64f);
            rt.anchoredPosition = new Vector2(x, 0f);

            go.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.22f, 0.92f);
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
            text.fontSize = 17;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;

            return x + 140f;
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

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
