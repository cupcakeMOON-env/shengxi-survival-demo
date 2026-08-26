using System;
using System.Collections.Generic;

namespace ShengXi.Simulation
{
    public struct WaveConfig
    {
        public int Count;
        public int Hp;
        public int Damage;
    }

    /// <summary>
    /// 波次调度：敌人数量与血量随天数增长，第 WinDay 天是 Boss 波。
    /// </summary>
    public static class WaveScheduler
    {
        public const int WinDay = 7;

        public static readonly IReadOnlyList<GridPos> DefaultSpawnPoints = new List<GridPos>
        {
            new GridPos(1, 15),
            new GridPos(28, 15),
            new GridPos(15, 1),
            new GridPos(15, 28),
        };

        /// <summary>
        /// 根据据点位置生成自适应刷怪点：
        /// 据点所在行/列的四条边各取一个可行走格子（优先正对据点，被水挡住就向两侧扩展）。
        /// </summary>
        public static IReadOnlyList<GridPos> SpawnPointsFor(GridMap map, GridPos basePos)
        {
            return new List<GridPos>
            {
                FindEdgeSpawn(map, 0, basePos.Y, map.Height, isVertical: true),
                FindEdgeSpawn(map, map.Width - 1, basePos.Y, map.Height, isVertical: true),
                FindEdgeSpawn(map, 0, basePos.X, map.Width, isVertical: false),
                FindEdgeSpawn(map, map.Height - 1, basePos.X, map.Width, isVertical: false),
            };
        }

        private static GridPos FindEdgeSpawn(GridMap map, int edge, int preferred, int count, bool isVertical)
        {
            var first = isVertical ? new GridPos(edge, preferred) : new GridPos(preferred, edge);
            if (map.GetTile(first) != null && map.GetTile(first).IsWalkable)
            {
                return first;
            }

            // 正对的格子不可走（比如是水），向两侧扩展
            for (var offset = 1; offset < count; offset++)
            {
                foreach (var sign in new[] { -1, 1 })
                {
                    var index = preferred + sign * offset;
                    if (index < 0 || index >= count)
                    {
                        continue;
                    }

                    var pos = isVertical ? new GridPos(edge, index) : new GridPos(index, edge);
                    if (map.GetTile(pos) != null && map.GetTile(pos).IsWalkable)
                    {
                        return pos;
                    }
                }
            }

            // 整条边扫描兜底
            for (var i = 0; i < count; i++)
            {
                var pos = isVertical ? new GridPos(edge, i) : new GridPos(i, edge);
                if (map.GetTile(pos) != null && map.GetTile(pos).IsWalkable)
                {
                    return pos;
                }
            }

            return first;
        }

        public static WaveConfig GetConfig(int day)
        {
            if (day >= WinDay)
            {
                return new WaveConfig { Count = 6, Hp = 8 + day * 2, Damage = 2 };
            }

            return new WaveConfig
            {
                Count = Math.Min(2 + day, 8),
                Hp = 1 + day / 2,
                Damage = 1,
            };
        }

        public static List<Enemy> SpawnWave(GridMap map, int day, int startId, IReadOnlyList<GridPos> spawnPoints)
        {
            var config = GetConfig(day);
            var enemies = new List<Enemy>();
            for (var i = 0; i < config.Count; i++)
            {
                var pos = spawnPoints[i % spawnPoints.Count];
                enemies.Add(new Enemy(startId + i, pos, config.Hp, config.Damage));
            }

            return enemies;
        }
    }
}
