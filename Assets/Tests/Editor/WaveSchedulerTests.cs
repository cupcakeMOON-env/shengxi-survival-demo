using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class WaveSchedulerTests
    {
        [Test]
        public void DayOne_WaveHasEnemies()
        {
            var config = WaveScheduler.GetConfig(1);

            Assert.That(config.Count, Is.GreaterThan(0));
            Assert.That(config.Hp, Is.GreaterThan(0));
            Assert.That(config.Damage, Is.GreaterThan(0));
        }

        [Test]
        public void LaterDays_AreHarder()
        {
            var day3 = WaveScheduler.GetConfig(3);
            var day6 = WaveScheduler.GetConfig(6);

            Assert.That(day6.Hp, Is.GreaterThanOrEqualTo(day3.Hp));
            Assert.That(day6.Count, Is.GreaterThanOrEqualTo(day3.Count));
        }

        [Test]
        public void FinalDay_UsesBossWave()
        {
            var boss = WaveScheduler.GetConfig(WaveScheduler.WinDay);

            Assert.That(boss.Damage, Is.EqualTo(2));
            Assert.That(boss.Hp, Is.GreaterThan(10));
        }

        [Test]
        public void SpawnWave_CreatesExpectedCount()
        {
            var map = new GridMap(30, 30);

            var enemies = WaveScheduler.SpawnWave(map, 1, 0, WaveScheduler.DefaultSpawnPoints);

            Assert.That(enemies.Count, Is.EqualTo(WaveScheduler.GetConfig(1).Count));
            Assert.That(enemies[0].Id, Is.EqualTo(0));
            Assert.That(enemies[1].Id, Is.EqualTo(1));
        }

        [Test]
        public void SpawnPointsFor_ReturnsWalkableEdgePoints()
        {
            var map = MapGenerator.CreateRandomMap(30, 30, 5);
            var basePos = new GridPos(15, 15);
            map.SetTerrain(basePos, TerrainType.Grass, 0); // 模拟游戏内 ConfirmBase 清地基
            map.Place(BuildingType.Base, basePos);

            var points = WaveScheduler.SpawnPointsFor(map, basePos);

            Assert.That(points.Count, Is.EqualTo(4));
            Assert.That(points[0].X, Is.EqualTo(0), "左边缘");
            Assert.That(points[1].X, Is.EqualTo(29), "右边缘");
            Assert.That(points[2].Y, Is.EqualTo(0), "下边缘");
            Assert.That(points[3].Y, Is.EqualTo(29), "上边缘");

            foreach (var point in points)
            {
                Assert.That(map.GetTile(point).IsWalkable, Is.True, $"刷怪点应可行走：{point}");
                Assert.That(map.GetTile(point).Building, Is.EqualTo(BuildingType.None), $"刷怪点应无建筑：{point}");
            }
        }

        [Test]
        public void SpawnPointsFor_HandlesNonWalkablePreferredTile()
        {
            // 人为把左边缘正对据点的格子设为水，验证会向两侧找到可行走点
            var map = new GridMap(10, 10);
            map.SetTerrain(new GridPos(0, 5), TerrainType.Water, 0);
            map.SetTerrain(new GridPos(1, 5), TerrainType.Water, 0);
            map.SetTerrain(new GridPos(0, 6), TerrainType.Water, 0);

            var points = WaveScheduler.SpawnPointsFor(map, new GridPos(5, 5));

            Assert.That(points[0].X, Is.EqualTo(0));
            Assert.That(map.GetTile(points[0]).IsWalkable, Is.True, "应避开水面");
        }

        [Test]
        public void SpawnPointsFor_SkipsWaterIsolatedTile_AndPrefersReachableOne()
        {
            var map = new GridMap(10, 10);
            map.SetTerrain(new GridPos(1, 5), TerrainType.Water, 0);
            map.SetTerrain(new GridPos(0, 4), TerrainType.Water, 0);
            map.SetTerrain(new GridPos(0, 6), TerrainType.Water, 0); // (0,5) 可行走但被水完全隔离

            var points = WaveScheduler.SpawnPointsFor(map, new GridPos(5, 5));

            Assert.That(points[0].X, Is.EqualTo(0), "仍应在左边缘");
            Assert.That(map.GetTile(points[0]).IsWalkable, Is.True);
            Assert.That(
                Pathfinding.IsReachable(map, points[0], new GridPos(5, 5)),
                Is.True,
                "应跳过孤立水盆格，选与据点连通的格子");
        }

        [Test]
        public void SpawnPointsFor_AlwaysReachableFromBase_AcrossSeeds()
        {
            for (var seed = 1; seed <= 200; seed++)
            {
                var map = MapGenerator.CreateRandomMap(MapGenerator.DefaultWidth, MapGenerator.DefaultHeight, seed);
                var basePos = FirstPlaceableTile(map);
                Assert.That(basePos.HasValue, Is.True, $"seed={seed} 应存在可放置据点处");
                var b = basePos.Value;
                map.SetTerrain(b, TerrainType.Grass, 0);
                map.Place(BuildingType.Base, b);

                var spawns = WaveScheduler.SpawnPointsFor(map, b);
                AssertSpawnEdgeReachability(map, b, spawns[0], 0, b.Y, map.Height, isVertical: true);
                AssertSpawnEdgeReachability(map, b, spawns[1], map.Width - 1, b.Y, map.Height, isVertical: true);
                AssertSpawnEdgeReachability(map, b, spawns[2], 0, b.X, map.Width, isVertical: false);
                AssertSpawnEdgeReachability(map, b, spawns[3], map.Height - 1, b.X, map.Width, isVertical: false);
            }
        }

        private static void AssertSpawnEdgeReachability(
            GridMap map,
            GridPos basePos,
            GridPos spawn,
            int edge,
            int preferred,
            int count,
            bool isVertical)
        {
            var reachable = Pathfinding.IsReachable(map, spawn, basePos);
            var edgeHasReachableOption = EdgeHasReachableSpawn(map, edge, preferred, count, isVertical, basePos);
            Assert.That(
                reachable,
                Is.EqualTo(edgeHasReachableOption),
                $"spawn={spawn} base={basePos}：边上有连通候选时刷怪点必须连通；否则才允许兜底");
        }

        /// <summary>该边是否存在「可行走且与据点连通」的候选刷怪格（与 FindEdgeSpawn 的候选集合一致）。</summary>
        private static bool EdgeHasReachableSpawn(
            GridMap map,
            int edge,
            int preferred,
            int count,
            bool isVertical,
            GridPos basePos)
        {
            for (var offset = 0; offset < count; offset++)
            {
                foreach (var sign in new[] { -1, 1 })
                {
                    var index = preferred + sign * offset;
                    if (index < 0 || index >= count)
                    {
                        continue;
                    }

                    var pos = isVertical ? new GridPos(edge, index) : new GridPos(index, edge);
                    var tile = map.GetTile(pos);
                    if (tile != null && tile.IsWalkable && Pathfinding.IsReachable(map, pos, basePos))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static GridPos? FirstPlaceableTile(GridMap map)
        {
            for (var x = 0; x < map.Width; x++)
            {
                for (var y = 0; y < map.Height; y++)
                {
                    var pos = new GridPos(x, y);
                    if (BasePlacementValidator.CanPlace(map, pos))
                    {
                        return pos;
                    }
                }
            }

            return null;
        }
    }
}
