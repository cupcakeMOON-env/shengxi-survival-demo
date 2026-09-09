using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class UpgradeServiceTests
    {
        private static (GridMap map, ResourcePool pool, ActionPointSystem ap) Setup()
        {
            var map = new GridMap(12, 12);
            var pool = new ResourcePool();
            pool.Add(ResourceType.Material, 30);
            return (map, pool, new ActionPointSystem(10));
        }

        [Test]
        public void Upgrade_ArrowTower_RaisesLevelDamageAndRepairs()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(6, 5);
            map.Place(BuildingType.ArrowTower, pos);
            map.SetBuildingHp(pos, 2); // 模拟夜晚被打残
            var def = BuildingCatalog.Get(BuildingType.ArrowTower);
            Assert.That(UpgradeService.CanUpgrade(map, pool, ap, pos), Is.True);

            Assert.That(UpgradeService.TryUpgrade(map, pool, ap, pos), Is.True);

            Assert.That(map.GetTile(pos).BuildingLevel, Is.EqualTo(2));
            Assert.That(map.GetTile(pos).BuildingHp, Is.EqualTo(BuildingStats.MaxHp(def, 2)), "升级应整修至新等级满血");
            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(30 - def.UpgradeMaterialCost));
            Assert.That(ap.Current, Is.EqualTo(9), "升级应消耗 1 行动点");
            Assert.That(BuildingStats.Damage(def, map.GetTile(pos).BuildingLevel), Is.EqualTo(2), "2 级箭塔伤害应 +1");
        }

        [Test]
        public void Upgrade_Wall_RaisesMaxHp()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(6, 7);
            map.Place(BuildingType.Wall, pos);
            var def = BuildingCatalog.Get(BuildingType.Wall);

            Assert.That(UpgradeService.TryUpgrade(map, pool, ap, pos), Is.True);

            Assert.That(map.GetTile(pos).BuildingLevel, Is.EqualTo(2));
            Assert.That(map.GetTile(pos).BuildingHp, Is.EqualTo(BuildingStats.MaxHp(def, 2)));
            Assert.That(BuildingStats.MaxHp(def, 2), Is.GreaterThan(def.MaxHp), "升级后的围墙应更厚");
        }

        [Test]
        public void Upgrade_NonUpgradeableBuilding_Fails()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(3, 3);
            map.Place(BuildingType.Warehouse, pos);

            Assert.That(UpgradeService.CanUpgrade(map, pool, ap, pos), Is.False, "仓库不可升级");
            Assert.That(UpgradeService.TryUpgrade(map, pool, ap, pos), Is.False);
            Assert.That(map.GetTile(pos).BuildingLevel, Is.EqualTo(1));
        }

        [Test]
        public void Upgrade_AtMaxLevel_Fails()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(6, 7);
            map.Place(BuildingType.Wall, pos);

            Assert.That(UpgradeService.TryUpgrade(map, pool, ap, pos), Is.True);
            Assert.That(UpgradeService.TryUpgrade(map, pool, ap, pos), Is.True);
            Assert.That(map.GetTile(pos).BuildingLevel, Is.EqualTo(UpgradeService.MaxLevel));

            Assert.That(UpgradeService.CanUpgrade(map, pool, ap, pos), Is.False, "满级不能再升");
            Assert.That(UpgradeService.TryUpgrade(map, pool, ap, pos), Is.False);
            Assert.That(map.GetTile(pos).BuildingLevel, Is.EqualTo(UpgradeService.MaxLevel));
        }

        [Test]
        public void Upgrade_WithoutMaterial_FailsAndKeepsState()
        {
            var (map, _, ap) = Setup();
            var pos = new GridPos(6, 5);
            map.Place(BuildingType.ArrowTower, pos);

            Assert.That(UpgradeService.CanUpgrade(map, new ResourcePool(), ap, pos), Is.False);
            Assert.That(UpgradeService.TryUpgrade(map, new ResourcePool(), ap, pos), Is.False);
            Assert.That(map.GetTile(pos).BuildingLevel, Is.EqualTo(1));
            Assert.That(ap.Current, Is.EqualTo(10), "失败不应扣行动点");
        }

        [Test]
        public void Upgrade_WithoutActionPoint_Fails()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(6, 5);
            map.Place(BuildingType.ArrowTower, pos);
            ap.Spend(10);

            Assert.That(UpgradeService.TryUpgrade(map, pool, ap, pos), Is.False);
            Assert.That(map.GetTile(pos).BuildingLevel, Is.EqualTo(1));
            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(30), "失败不应消耗建材");
        }

        [Test]
        public void Upgrade_EmptyTileAndBase_Fail()
        {
            var (map, pool, ap) = Setup();
            var basePos = new GridPos(6, 6);
            map.Place(BuildingType.Base, basePos);

            Assert.That(UpgradeService.CanUpgrade(map, pool, ap, new GridPos(1, 1)), Is.False);
            Assert.That(UpgradeService.CanUpgrade(map, pool, ap, basePos), Is.False, "据点不可升级");
            Assert.That(UpgradeService.TryUpgrade(map, pool, ap, basePos), Is.False);
        }

        [Test]
        public void Upgrade_RaisesBuildingUpgradedEvent()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(6, 5);
            map.Place(BuildingType.ArrowTower, pos);
            GridPos? observedPos = null;
            BuildingType? observedType = null;
            var observedLevel = 0;

            GameEvents.BuildingUpgraded += Handler;
            try
            {
                Assert.That(UpgradeService.TryUpgrade(map, pool, ap, pos), Is.True);
            }
            finally
            {
                GameEvents.BuildingUpgraded -= Handler;
            }

            Assert.That(observedPos, Is.EqualTo(pos));
            Assert.That(observedType, Is.EqualTo(BuildingType.ArrowTower));
            Assert.That(observedLevel, Is.EqualTo(2));

            void Handler(GridPos p, BuildingType building, int level)
            {
                observedPos = p;
                observedType = building;
                observedLevel = level;
            }
        }

        [Test]
        public void Upgrade_NullArguments_Fail()
        {
            Assert.That(UpgradeService.CanUpgrade(null, new ResourcePool(), new ActionPointSystem(10), new GridPos(1, 1)), Is.False);
            Assert.That(UpgradeService.TryUpgrade(null, null, null, new GridPos(1, 1)), Is.False);
        }
    }
}
