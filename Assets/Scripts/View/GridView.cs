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
        [SerializeField] private Color wallColor = new Color(0.45f, 0.38f, 0.32f);
        [SerializeField] private Color baseColor = new Color(0.95f, 0.82f, 0.30f);

        private const float TileSize = 0.96f;

        private readonly Dictionary<GridPos, SpriteRenderer> _tiles = new Dictionary<GridPos, SpriteRenderer>();
        private Sprite _unitSprite;
        private GridMap _map;
        private Transform _tileRoot;

        private void OnEnable()
        {
            GameEvents.MapInitialized += OnMapInitialized;
            GameEvents.TerrainChanged += OnTerrainChanged;
            GameEvents.BuildingPlaced += OnBuildingPlaced;
            GameEvents.BuildingRemoved += OnBuildingRemoved;

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

            renderer.color = building == BuildingType.Wall ? wallColor : ColorForTile(_map.GetTile(pos));
        }

        private void OnBuildingRemoved(GridPos pos)
        {
            if (!_tiles.TryGetValue(pos, out var renderer) || _map == null)
            {
                return;
            }

            renderer.color = ColorForTile(_map.GetTile(pos));
        }

        private void Build(GridMap map)
        {
            _map = map;
            Clear();
            EnsureTileRoot();

            if (_unitSprite == null)
            {
                _unitSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0, 0, 1, 1),
                    new Vector2(0.5f, 0.5f),
                    1f);
            }

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
                }
            }
        }

        private void Clear()
        {
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

        private Color ColorForTile(Tile tile) =>
            tile != null && tile.Building == BuildingType.Base ? baseColor : ColorFor(tile.Terrain);
    }
}
