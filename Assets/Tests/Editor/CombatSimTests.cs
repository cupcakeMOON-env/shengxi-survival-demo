using System.Collections.Generic;
using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class CombatSimTests
    {
        private static (GridMap map, ResourcePool pool, ActionPointSystem ap, Base baseDefense) Setup()
        {
            var map = new GridMap(12, 12);
            var pool = new ResourcePool();
            var baseDefense = new Base(new GridPos(6, 6), 20);
            map.Place(BuildingType.Base, baseDefense.Position);
            return (map, pool, new ActionPointSystem(10), baseDefense);
        }

        [Test]
        public void TowerKillsEnemyInRange()
        {
            var (map, pool, _, baseDefense) = Setup();
            map.Place(BuildingType.ArrowTower, new GridPos(6, 5));
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(6, 3), 1, 1) };

            CombatSim.Tick(map, pool, baseDefense, enemies);

            Assert.That(enemies.Count, Is.Zero);
            Assert.That(baseDefense.CurrentHp, Is.EqualTo(20));
        }

        [Test]
        public void EnemyReachesBase_DealsDamage()
        {
            var (map, pool, _, baseDefense) = Setup();
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(7, 6), 5, 1) };

            CombatSim.Tick(map, pool, baseDefense, enemies);

            Assert.That(enemies.Count, Is.Zero);
            Assert.That(baseDefense.CurrentHp, Is.EqualTo(19));
        }

        [Test]
        public void EnemyAdjacentToWall_AttacksWallInsteadOfBase()
        {
            var (map, pool, _, baseDefense) = Setup();
            map.Place(BuildingType.Wall, new GridPos(6, 7));
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(6, 8), 10, 1) };

            CombatSim.Tick(map, pool, baseDefense, enemies);

            Assert.That(enemies.Count, Is.EqualTo(1));
            Assert.That(enemies[0].Position, Is.EqualTo(new GridPos(6, 8)), "贴墙时原地攻击");
            Assert.That(map.GetTile(new GridPos(6, 7)).Building, Is.EqualTo(BuildingType.Wall));
            Assert.That(map.GetTile(new GridPos(6, 7)).BuildingHp, Is.EqualTo(9), "围墙 10 血 - 伤害 1");
            Assert.That(baseDefense.CurrentHp, Is.EqualTo(20), "有墙可打时本回合不该扣据点血");
        }

        [Test]
        public void WallBlocksDirectPath_EnemyMovesTowardWallNotThrough()
        {
            var (map, pool, _, baseDefense) = Setup();
            map.Place(BuildingType.Wall, new GridPos(6, 7));
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(6, 9), 10, 1) };

            CombatSim.Tick(map, pool, baseDefense, enemies);

            Assert.That(enemies[0].Position, Is.EqualTo(new GridPos(6, 8)), "应朝墙的贴邻格移动");
            Assert.That(map.GetTile(new GridPos(6, 7)).BuildingHp, Is.EqualTo(10), "未贴身不应扣建筑血");
            Assert.That(baseDefense.CurrentHp, Is.EqualTo(20));
        }

        [Test]
        public void Enemy_PrefersNearestBuilding_ByManhattan()
        {
            var (map, pool, _, baseDefense) = Setup();
            map.Place(BuildingType.Wall, new GridPos(5, 4)); // 距敌人 1
            map.Place(BuildingType.Wall, new GridPos(8, 5)); // 距敌人 3
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(5, 5), 10, 1) };

            CombatSim.Tick(map, pool, baseDefense, enemies);

            Assert.That(map.GetTile(new GridPos(5, 4)).BuildingHp, Is.EqualTo(9), "应攻击更近的墙");
            Assert.That(map.GetTile(new GridPos(8, 5)).BuildingHp, Is.EqualTo(10), "更远的墙不受影响");
        }

        [Test]
        public void Enemy_DestroysWall_ThenTargetsBase()
        {
            var (map, pool, _, baseDefense) = Setup();
            map.Place(BuildingType.Wall, new GridPos(6, 7));
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(6, 8), 10, 1) };

            for (var i = 0; i < 10; i++)
            {
                CombatSim.Tick(map, pool, baseDefense, enemies);
            }

            Assert.That(map.GetTile(new GridPos(6, 7)).Building, Is.EqualTo(BuildingType.None), "10 tick 后墙应被摧毁");
            Assert.That(map.GetTile(new GridPos(6, 7)).BuildingHp, Is.Zero);
            Assert.That(baseDefense.CurrentHp, Is.EqualTo(20), "墙被摧毁前据点不该掉血");

            CombatSim.Tick(map, pool, baseDefense, enemies);
            CombatSim.Tick(map, pool, baseDefense, enemies);

            Assert.That(baseDefense.CurrentHp, Is.EqualTo(19), "无建筑后敌人应转攻据点");
            Assert.That(enemies.Count, Is.Zero, "攻击据点后敌人消失");
        }

        [Test]
        public void Enemy_AttacksTowerAndDamagesIt()
        {
            var (map, pool, _, baseDefense) = Setup();
            map.Place(BuildingType.ArrowTower, new GridPos(6, 5));
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(6, 4), 10, 2) };

            CombatSim.Tick(map, pool, baseDefense, enemies);

            Assert.That(map.GetTile(new GridPos(6, 5)).Building, Is.EqualTo(BuildingType.ArrowTower));
            Assert.That(map.GetTile(new GridPos(6, 5)).BuildingHp, Is.EqualTo(3), "箭塔 5 血 - 敌人伤害 2");
            Assert.That(enemies.Count, Is.EqualTo(1), "攻击建筑后敌人存活");
        }

        [Test]
        public void EnemyDestroysWarehouse_CapacityReturnsToDefault()
        {
            var (map, pool, ap, baseDefense) = Setup();
            var warehousePos = new GridPos(6, 5);
            pool.Add(ResourceType.Wood, 20);
            pool.Add(ResourceType.Stone, 10);
            Assert.That(
                BuildService.TryBuild(map, pool, ap, BuildingCatalog.Get(BuildingType.Warehouse), warehousePos),
                Is.True);
            Assert.That(pool.Capacity, Is.EqualTo(200));
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(6, 4), 10, 1) };

            for (var i = 0; i < 5; i++)
            {
                CombatSim.Tick(map, pool, baseDefense, enemies);
            }

            Assert.That(map.GetTile(warehousePos).Building, Is.EqualTo(BuildingType.None), "仓库应被摧毁");
            Assert.That(pool.Capacity, Is.EqualTo(ResourcePool.DefaultCapacity), "仓库被拆后容量应回落");
            Assert.That(enemies.Count, Is.EqualTo(1));
        }

        [Test]
        public void BaseDestroyed_WhenHpReachesZero()
        {
            var (map, pool, _, baseDefense) = Setup();
            baseDefense.TakeDamage(19);
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(7, 6), 5, 1) };

            CombatSim.Tick(map, pool, baseDefense, enemies);

            Assert.That(baseDefense.IsDestroyed, Is.True);
            Assert.That(enemies.Count, Is.Zero);
        }

        [Test]
        public void TowerDamage_StacksAcrossTicks()
        {
            var (map, pool, _, baseDefense) = Setup();
            map.Place(BuildingType.ArrowTower, new GridPos(6, 5));
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(6, 3), 2, 1) };

            CombatSim.Tick(map, pool, baseDefense, enemies);
            Assert.That(enemies.Count, Is.EqualTo(1), "2 血敌人第一 tick 只受 1 伤");
            CombatSim.Tick(map, pool, baseDefense, enemies);
            Assert.That(enemies.Count, Is.Zero, "第二 tick 被击杀");
        }

        [Test]
        public void ManhattanRange_HitsEnemyThreeStepsAway()
        {
            var (map, pool, _, baseDefense) = Setup();
            map.Place(BuildingType.ArrowTower, new GridPos(6, 5));
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(8, 6), 5, 1) };

            CombatSim.Tick(map, pool, baseDefense, enemies);

            Assert.That(enemies[0].HP, Is.EqualTo(4), "曼哈顿距离 |2|+|1|=3 应命中");
        }

        [Test]
        public void ManhattanRange_ExcludesEnemyFourStepsDiagonal()
        {
            var (map, pool, _, baseDefense) = Setup();
            map.Place(BuildingType.ArrowTower, new GridPos(6, 5));
            // |2|+|2|=4 > 3；若用旧的欧氏距离约 2.83 会命中，曼哈顿规则下不应命中
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(8, 7), 5, 1) };

            CombatSim.Tick(map, pool, baseDefense, enemies);

            Assert.That(enemies[0].HP, Is.EqualTo(5), "曼哈顿距离 4 不应命中");
        }
    }
}
