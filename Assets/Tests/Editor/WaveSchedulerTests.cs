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
    }
}
