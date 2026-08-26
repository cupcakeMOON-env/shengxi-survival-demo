using System.Collections.Generic;
using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class CombatSimTests
    {
        private static (GridMap map, Base baseDefense) Setup()
        {
            var map = new GridMap(12, 12);
            var baseDefense = new Base(new GridPos(6, 6), 20);
            map.Place(BuildingType.Base, baseDefense.Position);
            return (map, baseDefense);
        }

        [Test]
        public void TowerKillsEnemyInRange()
        {
            var (map, baseDefense) = Setup();
            map.Place(BuildingType.ArrowTower, new GridPos(6, 5));
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(6, 3), 1, 1) };

            CombatSim.Tick(map, baseDefense, enemies);

            Assert.That(enemies.Count, Is.Zero);
            Assert.That(baseDefense.CurrentHp, Is.EqualTo(20));
        }

        [Test]
        public void EnemyReachesBase_DealsDamage()
        {
            var (map, baseDefense) = Setup();
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(7, 6), 5, 1) };

            CombatSim.Tick(map, baseDefense, enemies);

            Assert.That(enemies.Count, Is.Zero);
            Assert.That(baseDefense.CurrentHp, Is.EqualTo(19));
        }

        [Test]
        public void WallBlocksDirectPath_EnemyGoesAround()
        {
            var (map, baseDefense) = Setup();
            map.Place(BuildingType.Wall, new GridPos(6, 7));
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(6, 8), 10, 1) };

            CombatSim.Tick(map, baseDefense, enemies);

            Assert.That(enemies.Count, Is.EqualTo(1));
            Assert.That(enemies[0].Position, Is.Not.EqualTo(new GridPos(6, 7)), "不能走进围墙");
            Assert.That(baseDefense.CurrentHp, Is.EqualTo(20), "围墙挡住时本回合不该扣据点血");
        }

        [Test]
        public void BaseDestroyed_WhenHpReachesZero()
        {
            var (map, baseDefense) = Setup();
            baseDefense.TakeDamage(19);
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(7, 6), 5, 1) };

            CombatSim.Tick(map, baseDefense, enemies);

            Assert.That(baseDefense.IsDestroyed, Is.True);
            Assert.That(enemies.Count, Is.Zero);
        }

        [Test]
        public void TowerDamage_StacksAcrossTicks()
        {
            var (map, baseDefense) = Setup();
            map.Place(BuildingType.ArrowTower, new GridPos(6, 5));
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(6, 3), 2, 1) };

            CombatSim.Tick(map, baseDefense, enemies);
            Assert.That(enemies.Count, Is.EqualTo(1), "2 血敌人第一 tick 只受 1 伤");
            CombatSim.Tick(map, baseDefense, enemies);
            Assert.That(enemies.Count, Is.Zero, "第二 tick 被击杀");
        }

        [Test]
        public void ManhattanRange_HitsEnemyThreeStepsAway()
        {
            var (map, baseDefense) = Setup();
            map.Place(BuildingType.ArrowTower, new GridPos(6, 5));
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(8, 6), 5, 1) };

            CombatSim.Tick(map, baseDefense, enemies);

            Assert.That(enemies[0].HP, Is.EqualTo(4), "曼哈顿距离 |2|+|1|=3 应命中");
        }

        [Test]
        public void ManhattanRange_ExcludesEnemyFourStepsDiagonal()
        {
            var (map, baseDefense) = Setup();
            map.Place(BuildingType.ArrowTower, new GridPos(6, 5));
            // |2|+|2|=4 > 3；若用旧的欧氏距离约 2.83 会命中，曼哈顿规则下不应命中
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(8, 7), 5, 1) };

            CombatSim.Tick(map, baseDefense, enemies);

            Assert.That(enemies[0].HP, Is.EqualTo(5), "曼哈顿距离 4 不应命中");
        }
    }
}
