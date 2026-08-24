using System;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 地图生成器。
    /// CreateDemoMap：固定演示布局（便于单测）；
    /// CreateRandomMap：噪声 + 规则生成，每局地图不同（M4）。
    /// </summary>
    public static class MapGenerator
    {
        public const int DefaultWidth = 30;
        public const int DefaultHeight = 30;

        /// <summary>
        /// 随机地图：海拔噪声决定水/陆地，湿度噪声决定森林，
        /// 再用随机聚类点缀石矿与浆果丛，最后保证据点中央是空地。
        /// </summary>
        public static GridMap CreateRandomMap(int width, int height, int seed)
        {
            var map = new GridMap(width, height);
            var rng = new System.Random(seed);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var pos = new GridPos(x, y);
                    var altitude = Noise.Fbm(x * 0.09f, y * 0.09f, seed, 3);
                    var moisture = Noise.Fbm(x * 0.12f + 31.7f, y * 0.12f + 17.3f, seed + 999, 3);

                    if (altitude < 0.34f)
                    {
                        map.SetTerrain(pos, TerrainType.Water, 0);
                    }
                    else if (moisture > 0.62f)
                    {
                        map.SetTerrain(pos, TerrainType.Forest, rng.Next(30, 61));
                    }
                    else
                    {
                        map.SetTerrain(pos, TerrainType.Grass, 0);
                    }
                }
            }

            // 石矿聚类
            var stoneBlobs = Math.Max(1, width * height / 300);
            for (var i = 0; i < stoneBlobs; i++)
            {
                var center = new GridPos(rng.Next(2, width - 2), rng.Next(2, height - 2));
                FillCircle(map, center, rng.Next(1, 3), TerrainType.Stone, rng.Next(20, 41));
            }

            // 浆果丛聚类
            var bushBlobs = Math.Max(1, width * height / 250);
            for (var i = 0; i < bushBlobs; i++)
            {
                var center = new GridPos(rng.Next(2, width - 2), rng.Next(2, height - 2));
                FillCircle(map, center, rng.Next(1, 2), TerrainType.Bush, rng.Next(15, 31));
            }

            // 据点中央清出一块空地
            FillCircle(map, new GridPos(width / 2, height / 2), 2, TerrainType.Grass, 0);
            return map;
        }

        public static GridMap CreateDemoMap(int width = DefaultWidth, int height = DefaultHeight)
        {
            var map = new GridMap(width, height);

            // 左上森林
            FillCircle(map, new GridPos(6, 23), 4, TerrainType.Forest, 50);
            // 右下森林
            FillCircle(map, new GridPos(23, 6), 3, TerrainType.Forest, 40);
            // 右侧石矿脉
            FillEllipse(map, new GridPos(25, 18), 3, 2, TerrainType.Stone, 30);
            // 左下池塘（不可通行）
            FillCircle(map, new GridPos(6, 6), 3, TerrainType.Water, 0);
            // 中部浆果丛（食物）
            FillCircle(map, new GridPos(10, 12), 2, TerrainType.Bush, 25);

            return map;
        }

        public static void FillCircle(GridMap map, GridPos center, int radius, TerrainType terrain, int resourceAmount)
        {
            for (var x = center.X - radius; x <= center.X + radius; x++)
            {
                for (var y = center.Y - radius; y <= center.Y + radius; y++)
                {
                    var pos = new GridPos(x, y);
                    if (!map.IsInside(pos))
                    {
                        continue;
                    }

                    var dx = x - center.X;
                    var dy = y - center.Y;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        map.SetTerrain(pos, terrain, resourceAmount);
                    }
                }
            }
        }

        public static void FillEllipse(GridMap map, GridPos center, int rx, int ry, TerrainType terrain, int resourceAmount)
        {
            for (var x = center.X - rx; x <= center.X + rx; x++)
            {
                for (var y = center.Y - ry; y <= center.Y + ry; y++)
                {
                    var pos = new GridPos(x, y);
                    if (!map.IsInside(pos))
                    {
                        continue;
                    }

                    var nx = (x - center.X) / (double)rx;
                    var ny = (y - center.Y) / (double)ry;
                    if (nx * nx + ny * ny <= 1.0)
                    {
                        map.SetTerrain(pos, terrain, resourceAmount);
                    }
                }
            }
        }
    }
}
