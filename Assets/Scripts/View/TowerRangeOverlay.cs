using System.Collections.Generic;
using ShengXi.Core;
using ShengXi.Simulation;
using UnityEngine;

namespace ShengXi.View
{
    /// <summary>
    /// 射程可视化：悬停已有箭塔（或建造模式选中箭塔）时，
    /// 高亮其曼哈顿射程内的所有格子——与战斗判定的规则完全一致，数据来自 BuildingDef.Range。
    /// </summary>
    public class TowerRangeOverlay : MonoBehaviour
    {
        private Camera _camera;
        private BuildModeController _buildMode;
        private readonly Dictionary<GridPos, SpriteRenderer> _overlays = new Dictionary<GridPos, SpriteRenderer>();
        private Sprite _sprite;
        private GridPos? _lastHovered;
        private BuildingType _lastContext;

        public void Initialize(Camera camera, BuildModeController buildMode)
        {
            _camera = camera;
            _buildMode = buildMode;
        }

        private void Update()
        {
            var loop = GameLoop.Instance;
            if (loop == null || loop.GameOver || _camera == null)
            {
                Clear();
                return;
            }

            var world = _camera.ScreenToWorldPoint(Input.mousePosition);
            var hovered = new GridPos(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
            var tile = loop.Map.GetTile(hovered);

            BuildingDef def = null;
            if (tile != null && tile.Building == BuildingType.ArrowTower)
            {
                def = BuildingCatalog.Get(BuildingType.ArrowTower);
            }
            else if (_buildMode != null &&
                     _buildMode.ActiveDef != null &&
                     _buildMode.ActiveDef.Type == BuildingType.ArrowTower)
            {
                def = _buildMode.ActiveDef;
            }

            if (def == null || def.Range <= 0)
            {
                Clear();
                return;
            }

            if (hovered == _lastHovered && _lastContext == BuildingType.ArrowTower)
            {
                return;
            }

            _lastHovered = hovered;
            _lastContext = BuildingType.ArrowTower;
            Rebuild(hovered, def.Range);
        }

        private void Rebuild(GridPos center, int range)
        {
            Clear();
            var map = GameLoop.Instance.Map;

            for (var dx = -range; dx <= range; dx++)
            {
                for (var dy = -range; dy <= range; dy++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) > range)
                    {
                        continue;
                    }

                    var pos = new GridPos(center.X + dx, center.Y + dy);
                    if (!map.IsInside(pos))
                    {
                        continue;
                    }

                    var go = new GameObject($"Range({pos.X},{pos.Y})");
                    go.transform.SetParent(transform, false);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = GetSprite();
                    sr.color = new Color(0.30f, 0.62f, 1f, 0.22f);
                    sr.transform.position = new Vector3(pos.X, pos.Y, -0.4f);
                    sr.transform.localScale = new Vector3(0.96f, 0.96f, 1f);
                    _overlays[pos] = sr;
                }
            }
        }

        private void Clear()
        {
            if (_overlays.Count > 0)
            {
                foreach (var sr in _overlays.Values)
                {
                    Destroy(sr.gameObject);
                }

                _overlays.Clear();
            }

            _lastHovered = null;
            _lastContext = BuildingType.None;
        }

        private Sprite GetSprite()
        {
            if (_sprite == null)
            {
                _sprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0, 0, 1, 1),
                    new Vector2(0.5f, 0.5f),
                    1f);
            }

            return _sprite;
        }
    }
}
