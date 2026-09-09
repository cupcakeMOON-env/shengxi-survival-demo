using System.Collections.Generic;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 网格地图：模拟层唯一的地图数据源。
    /// 纯 C#，不依赖 UnityEngine；查询、校验、写入都集中在这里，渲染交给 View 层。
    /// </summary>
    public class GridMap
    {
        private readonly Tile[,] _tiles;

        public int Width { get; }
        public int Height { get; }

        public GridMap(int width, int height)
        {
            Width = width;
            Height = height;
            _tiles = new Tile[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    _tiles[x, y] = new Tile(TerrainType.Grass);
                }
            }
        }

        public bool IsInside(GridPos pos) => pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;

        public Tile GetTile(GridPos pos) => IsInside(pos) ? _tiles[pos.X, pos.Y] : null;

        public void SetTerrain(GridPos pos, TerrainType terrain, int resourceAmount = 0)
        {
            if (!IsInside(pos))
            {
                return;
            }

            var tile = _tiles[pos.X, pos.Y];
            tile.Terrain = terrain;
            tile.ResourceAmount = resourceAmount;
        }

        public bool CanPlace(BuildingType building, GridPos pos)
        {
            if (!IsInside(pos))
            {
                return false;
            }

            var tile = _tiles[pos.X, pos.Y];
            return tile.Building == BuildingType.None && tile.IsWalkable;
        }

        public bool Place(BuildingType building, GridPos pos)
        {
            if (!CanPlace(building, pos))
            {
                return false;
            }

            var tile = _tiles[pos.X, pos.Y];
            tile.Building = building;
            tile.BuildingLevel = 1;
            var def = BuildingCatalog.Get(building);
            tile.BuildingHp = def != null && def.MaxHp > 0 ? def.MaxHp : 1;
            return true;
        }

        public void RemoveBuilding(GridPos pos)
        {
            if (!IsInside(pos))
            {
                return;
            }

            var tile = _tiles[pos.X, pos.Y];
            tile.Building = BuildingType.None;
            tile.BuildingHp = 0;
            tile.BuildingLevel = 0;
        }

        /// <summary>读档恢复建筑血量：直接设置，不发事件。</summary>
        public void SetBuildingHp(GridPos pos, int hp)
        {
            if (!IsInside(pos))
            {
                return;
            }

            _tiles[pos.X, pos.Y].BuildingHp = System.Math.Max(0, hp);
        }

        /// <summary>读档恢复建筑等级：直接设置，不发事件。</summary>
        public void SetBuildingLevel(GridPos pos, int level)
        {
            if (IsInside(pos))
            {
                _tiles[pos.X, pos.Y].BuildingLevel = System.Math.Max(0, level);
            }
        }

        public IEnumerable<GridPos> FindBuildingPositions(BuildingType type)
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    if (_tiles[x, y].Building == type)
                    {
                        yield return new GridPos(x, y);
                    }
                }
            }
        }
    }
}
