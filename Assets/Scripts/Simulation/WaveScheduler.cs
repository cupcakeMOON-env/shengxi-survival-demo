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
