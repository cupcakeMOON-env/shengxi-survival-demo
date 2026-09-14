using System.Collections;
using System.Collections.Generic;
using ShengXi.Core;
using ShengXi.Simulation;
using UnityEngine;

namespace ShengXi.View
{
    /// <summary>
    /// 网格表现层：把 GridMap 画成带间隙的色块，间隙露出背景即形成网格线。
    /// 只订阅事件并渲染，绝不直接修改模拟数据。
    /// </summary>
    public class GridView : MonoBehaviour
    {
        [SerializeField] private Color grassColor = new Color(0.32f, 0.52f, 0.24f);
        [SerializeField] private Color forestColor = new Color(0.16f, 0.40f, 0.16f);
        [SerializeField] private Color stoneColor = new Color(0.58f, 0.56f, 0.52f);
        [SerializeField] private Color waterColor = new Color(0.25f, 0.48f, 0.70f);
        [SerializeField] private Color bushColor = new Color(0.85f, 0.52f, 0.18f);
        [SerializeField] private Color collectorColor = new Color(0.30f, 0.75f, 0.55f);
        [SerializeField] private Color warehouseColor = new Color(0.62f, 0.45f, 0.28f);
        [SerializeField] private Color wallColor = new Color(0.60f, 0.58f, 0.56f);
        [SerializeField] private Color towerColor = new Color(0.35f, 0.55f, 0.90f);
        [SerializeField] private Color starvedColor = new Color(0.26f, 0.28f, 0.33f);
        [SerializeField] private Color workshopColor = new Color(0.95f, 0.62f, 0.20f);
        [SerializeField] private Color baseColor = new Color(0.95f, 0.82f, 0.30f);

        private const float TileSize = 0.96f;
        private const float FlashDuration = 0.18f;
        private const float BarWidth = 0.7f;
        private const float BarHeight = 0.1f;
        private const float BarYOffset = 0.56f;

        private readonly Dictionary<GridPos, SpriteRenderer> _tiles = new Dictionary<GridPos, SpriteRenderer>();
        private readonly Dictionary<GridPos, Coroutine> _flashes = new Dictionary<GridPos, Coroutine>();
        private readonly Dictionary<GridPos, BuildingHealthBar> _healthBars = new Dictionary<GridPos, BuildingHealthBar>();
        private Sprite _unitSprite;
        private GridMap _map;
        private Transform _tileRoot;

        private void OnEnable()
        {
            GameEvents.MapInitialized += OnMapInitialized;
            GameEvents.TerrainChanged += OnTerrainChanged;
            GameEvents.BuildingPlaced += OnBuildingPlaced;
            GameEvents.BuildingRemoved += OnBuildingRemoved;
            GameEvents.BuildingDamaged += OnBuildingDamaged;
            GameEvents.BuildingRepaired += OnBuildingRepaired;
            GameEvents.BuildingSupplyChanged += OnBuildingSupplyChanged;
            GameEvents.BuildingUpgraded += OnBuildingUpgraded;

            EnsureTileRoot();
            if (GameLoop.Instance != null)
            {
                Build(GameLoop.Instance.Map);
            }
        }

        private void OnDisable()
        {
            GameEvents.MapInitialized -= OnMapInitialized;
            GameEvents.TerrainChanged -= OnTerrainChanged;
            GameEvents.BuildingPlaced -= OnBuildingPlaced;
            GameEvents.BuildingRemoved -= OnBuildingRemoved;
            GameEvents.BuildingDamaged -= OnBuildingDamaged;
            GameEvents.BuildingRepaired -= OnBuildingRepaired;
            GameEvents.BuildingSupplyChanged -= OnBuildingSupplyChanged;
            GameEvents.BuildingUpgraded -= OnBuildingUpgraded;
        }

        private void OnMapInitialized(GridMap map) => Build(map);

        private void OnTerrainChanged(GridPos pos, TerrainType terrain)
        {
            if (_tiles.TryGetValue(pos, out var renderer))
            {
                renderer.color = ColorFor(terrain);
            }
        }

        private void OnBuildingPlaced(GridPos pos, BuildingType building)
        {
            if (!_tiles.TryGetValue(pos, out var renderer) || _map == null)
            {
                return;
            }

            renderer.color = ColorForTile(_map.GetTile(pos));
            EnsureHealthBar(pos, renderer);
        }

        private void OnBuildingRemoved(GridPos pos)
        {
            if (!_tiles.TryGetValue(pos, out var renderer) || _map == null)
            {
                return;
            }

            renderer.color = ColorForTile(_map.GetTile(pos));
            StopFlash(pos);
            DestroyHealthBar(pos);
        }

        private void OnBuildingDamaged(GridPos pos, int currentHp, int maxHp)
        {
            if (_healthBars.TryGetValue(pos, out var bar))
            {
                bar.SetRatio(maxHp > 0 ? (float)currentHp / maxHp : 0f);
            }

            FlashTile(pos);
        }

        /// <summary>修理完成：血条回到满格并闪一下，给玩家正反馈。</summary>
        private void OnBuildingRepaired(GridPos pos, int currentHp, int maxHp)
        {
            if (_healthBars.TryGetValue(pos, out var bar))
            {
                bar.SetRatio(maxHp > 0 ? (float)currentHp / maxHp : 0f);
            }

            FlashTile(pos);
        }

        /// <summary>断粮恢复/断粮：直接按格子当前数据重上色（事件由模拟层在改完状态后发出）。</summary>
        private void OnBuildingSupplyChanged(GridPos pos, bool supplied)
        {
            if (_tiles.TryGetValue(pos, out var renderer) && _map != null)
            {
                renderer.color = ColorForTile(_map.GetTile(pos));
            }
        }

        /// <summary>升级后重新上色（等级越高越亮）并按新等级血量刷新血条。</summary>
        private void OnBuildingUpgraded(GridPos pos, BuildingType building, int level)
        {
            if (!_tiles.TryGetValue(pos, out var renderer) || _map == null)
            {
                return;
            }

            renderer.color = ColorForTile(_map.GetTile(pos));
            var tile = _map.GetTile(pos);
            var def = BuildingCatalog.Get(tile.Building);
            var maxHp = BuildingStats.MaxHp(def, tile.BuildingLevel);
            if (_healthBars.TryGetValue(pos, out var bar))
            {
                bar.SetRatio(maxHp > 0 ? (float)tile.BuildingHp / maxHp : 0f);
            }
        }

        private void Build(GridMap map)
        {
            _map = map;
            Clear();
            EnsureTileRoot();

            EnsureUnitSprite();

            for (var x = 0; x < map.Width; x++)
            {
                for (var y = 0; y < map.Height; y++)
                {
                    var pos = new GridPos(x, y);
                    var go = new GameObject($"Tile({x},{y})");
                    go.transform.SetParent(_tileRoot, false);
                    go.transform.localPosition = new Vector3(x, y, 0f);

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = _unitSprite;
                    sr.color = ColorForTile(map.GetTile(pos));
                    sr.transform.localScale = new Vector3(TileSize, TileSize, 1f);
                    _tiles[pos] = sr;
                    if (map.GetTile(pos).Building != BuildingType.None)
                    {
                        EnsureHealthBar(pos, sr);
                    }
                }
            }
        }

        private void Clear()
        {
            foreach (var coroutine in _flashes.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }

            _flashes.Clear();
            _healthBars.Clear();
            if (_tileRoot == null)
            {
                return;
            }

            for (var i = _tileRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_tileRoot.GetChild(i).gameObject);
            }

            _tiles.Clear();
        }

        private void EnsureUnitSprite()
        {
            if (_unitSprite != null)
            {
                return;
            }

            _unitSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f),
                1f);
        }

        /// <summary>受击闪白：格子颜色从本色脉冲到白色再恢复（每次受击重置动画）。</summary>
        private void FlashTile(GridPos pos)
        {
            if (_flashes.TryGetValue(pos, out var running) && running != null)
            {
                StopCoroutine(running);
            }

            _flashes[pos] = StartCoroutine(FlashTileRoutine(pos));
        }

        private void StopFlash(GridPos pos)
        {
            if (_flashes.TryGetValue(pos, out var running) && running != null)
            {
                StopCoroutine(running);
            }

            _flashes.Remove(pos);
        }

        private IEnumerator FlashTileRoutine(GridPos pos)
        {
            if (!_tiles.TryGetValue(pos, out var renderer) || _map == null)
            {
                _flashes.Remove(pos);
                yield break;
            }

            var baseColor = ColorForTile(_map.GetTile(pos));
            var elapsed = 0f;
            while (elapsed < FlashDuration)
            {
                elapsed += Time.deltaTime;
                var pulse = 1f - Mathf.Abs(elapsed / FlashDuration * 2f - 1f);
                renderer.color = Color.Lerp(baseColor, Color.white, pulse);
                yield return null;
            }

            // 闪烁期间状态可能变化（断粮/恢复），结束以最新数据上色而不是旧底色
            renderer.color = ColorForTile(_map.GetTile(pos));
            _flashes.Remove(pos);
        }

        /// <summary>为有血量的建筑格子创建血条（位于格子上方，随血量左右收缩）。</summary>
        private void EnsureHealthBar(GridPos pos, SpriteRenderer tileRenderer)
        {
            if (_healthBars.ContainsKey(pos) || _map == null)
            {
                return;
            }

            var def = BuildingCatalog.Get(_map.GetTile(pos).Building);
            if (def == null || def.MaxHp <= 0)
            {
                return;
            }

            EnsureUnitSprite();
            var bar = new BuildingHealthBar
            {
                Width = BarWidth,
                Background = CreateBarSprite(tileRenderer.transform, new Color(0f, 0f, 0f, 0.65f), 1f),
                Fill = CreateBarSprite(tileRenderer.transform, new Color(0.38f, 0.88f, 0.32f), 1f),
            };
            bar.SetRatio((float)_map.GetTile(pos).BuildingHp / BuildingStats.MaxHp(def, _map.GetTile(pos).BuildingLevel));
            _healthBars[pos] = bar;
        }

        private SpriteRenderer CreateBarSprite(Transform parent, Color color, float ratio)
        {
            var go = new GameObject("HpBar");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _unitSprite;
            sr.color = color;
            sr.transform.localScale = new Vector3(BarWidth * ratio, BarHeight, 1f);
            sr.transform.localPosition = new Vector3(BarWidth * (ratio - 1f) * 0.5f, BarYOffset, -0.1f);
            return sr;
        }

        private void DestroyHealthBar(GridPos pos)
        {
            if (!_healthBars.TryGetValue(pos, out var bar))
            {
                return;
            }

            if (bar.Background != null)
            {
                Destroy(bar.Background.gameObject);
            }

            if (bar.Fill != null)
            {
                Destroy(bar.Fill.gameObject);
            }

            _healthBars.Remove(pos);
        }

        /// <summary>
        /// 格子统一挂在独立的 Grid 容器下，避免与同节点的其他组件（幽灵预览、敌人）混在一起。
        /// </summary>
        private void EnsureTileRoot()
        {
            if (_tileRoot != null)
            {
                return;
            }

            var rootGo = new GameObject("Grid");
            rootGo.transform.SetParent(transform, false);
            _tileRoot = rootGo.transform;
        }

        private Color ColorFor(TerrainType terrain) => terrain switch
        {
            TerrainType.Forest => forestColor,
            TerrainType.Stone => stoneColor,
            TerrainType.Water => waterColor,
            TerrainType.Bush => bushColor,
            _ => grassColor,
        };

        /// <summary>格子颜色优先级：建筑 > 地形；每种建筑有独立颜色，方便一眼区分。</summary>
        private Color ColorForTile(Tile tile)
        {
            if (tile == null)
            {
                return grassColor;
            }

            var buildingColor = ColorForBuilding(tile.Building);
            if (buildingColor.HasValue)
            {
                // 需要食物的防御建筑断粮时统一变灰（当前只有箭塔）
                if (tile.BuildingStarved)
                {
                    var def = BuildingCatalog.Get(tile.Building);
                    if (def != null && def.FoodPerSecond > 0)
                    {
                        return starvedColor;
                    }
                }

                // 等级越高的建筑颜色越亮，升级效果一眼可见（等级 1 保持原始色）
                if (tile.BuildingLevel > 1)
                {
                    buildingColor = Color.Lerp(
                        buildingColor.Value,
                        Color.white,
                        Mathf.Min(0.4f, 0.15f * (tile.BuildingLevel - 1)));
                }

                return buildingColor.Value;
            }

            return ColorFor(tile.Terrain);
        }

        private Color? ColorForBuilding(BuildingType building) => building switch
        {
            BuildingType.Base => baseColor,
            BuildingType.Collector => collectorColor,
            BuildingType.Warehouse => warehouseColor,
            BuildingType.Wall => wallColor,
            BuildingType.ArrowTower => towerColor,
            BuildingType.Workshop => workshopColor,
            _ => null,
        };

        /// <summary>建筑血条：底黑条 + 前景填充，填充从左侧收缩。</summary>
        private class BuildingHealthBar
        {
            public SpriteRenderer Background;
            public SpriteRenderer Fill;
            public float Width;

            public void SetRatio(float ratio)
            {
                ratio = Mathf.Clamp01(ratio);
                Fill.transform.localScale = new Vector3(Width * ratio, BarHeight, 1f);
                Fill.transform.localPosition = new Vector3(Width * (ratio - 1f) * 0.5f, BarYOffset, -0.1f);
            }
        }
    }
}
