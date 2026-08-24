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
    }
}
