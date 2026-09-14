using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class RepairServiceTests
    {
        private static (GridMap map, ResourcePool pool, ActionPointSystem ap) Setup(int kits = 3)
        {
            var map = new GridMap(12, 12);
            var pool = new ResourcePool();
            pool.Add(ResourceType.RepairKit, kits);
            return (map, pool, new ActionPointSystem(10));
        }

        [Test]
        public void Repair_Building_RestoresFullHp_SpendsKitAndActionPoint()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(6, 7);
            map.Place(BuildingType.Wall, pos);
            map.SetBuildingHp(pos, 4);
            Assert.That(RepairService.CanRepairBuilding(map, pool, ap, pos), Is.True);

            Assert.That(RepairService.TryRepairBuilding(map, pool, ap, pos), Is.True);

            var def = BuildingCatalog.Get(BuildingType.Wall);
            Assert.That(map.GetTile(pos).BuildingHp, Is.EqualTo(BuildingStats.MaxHp(def, 1)), "修理应回满血");
            Assert.That(pool.GetAmount(ResourceType.RepairKit), Is.EqualTo(2), "应消耗 1 修理包");
            Assert.That(ap.Current, Is.EqualTo(9), "应消耗 1 行动点");
        }

        [Test]
        public void Repair_UpgradedBuilding_RestoresToUpgradedMaxHp()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(6, 7);
            map.Place(BuildingType.Wall, pos);
            map.SetBuildingLevel(pos, 3);
            map.SetBuildingHp(pos, 1);
            var def = BuildingCatalog.Get(BuildingType.Wall);
            var maxHp = BuildingStats.MaxHp(def, 3);

            Assert.That(RepairService.TryRepairBuilding(map, pool, ap, pos), Is.True);

            Assert.That(map.GetTile(pos).BuildingHp, Is.EqualTo(maxHp), "应按当前等级的上限修满");
            Assert.That(maxHp, Is.GreaterThan(def.MaxHp));
        }

        [Test]
        public void Repair_FullHpBuilding_Fails()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(6, 7);
            map.Place(BuildingType.Wall, pos);

            Assert.That(RepairService.CanRepairBuilding(map, pool, ap, pos), Is.False, "满血不需要修");
            Assert.That(RepairService.TryRepairBuilding(map, pool, ap, pos), Is.False);
            Assert.That(pool.GetAmount(ResourceType.RepairKit), Is.EqualTo(3), "失败不应消耗修理包");
            Assert.That(ap.Current, Is.EqualTo(10));
        }

        [Test]
        public void Repair_WithoutKit_Fails()
        {
            var (map, _, ap) = Setup(kits: 0);
            var pos = new GridPos(6, 7);
            map.Place(BuildingType.Wall, pos);
            map.SetBuildingHp(pos, 3);

            Assert.That(RepairService.CanRepairBuilding(map, new ResourcePool(), ap, pos), Is.False);
            Assert.That(RepairService.TryRepairBuilding(map, new ResourcePool(), ap, pos), Is.False);
            Assert.That(map.GetTile(pos).BuildingHp, Is.EqualTo(3), "失败不应改血量");
            Assert.That(ap.Current, Is.EqualTo(10), "失败不应扣行动点");
        }

        [Test]
        public void Repair_WithoutActionPoint_Fails()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(6, 7);
            map.Place(BuildingType.Wall, pos);
            map.SetBuildingHp(pos, 3);
            ap.Spend(10);

            Assert.That(RepairService.TryRepairBuilding(map, pool, ap, pos), Is.False);
            Assert.That(pool.GetAmount(ResourceType.RepairKit), Is.EqualTo(3), "失败不应消耗修理包");
            Assert.That(map.GetTile(pos).BuildingHp, Is.EqualTo(3));
        }

        [Test]
        public void Repair_EmptyTileAndBaseTile_Fail()
        {
            var (map, pool, ap) = Setup();
            var basePos = new GridPos(6, 6);
            map.Place(BuildingType.Base, basePos);

            Assert.That(RepairService.CanRepairBuilding(map, pool, ap, new GridPos(1, 1)), Is.False, "空地不能修");
            Assert.That(RepairService.CanRepairBuilding(map, pool, ap, basePos), Is.False, "据点不在地图建筑目录里，走 CanRepairBase");
        }

        [Test]
        public void Repair_RaisesBuildingRepairedEvent()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(6, 7);
            map.Place(BuildingType.Wall, pos);
            map.SetBuildingHp(pos, 2);
            GridPos? observedPos = null;
            var observedHp = 0;
            var observedMax = 0;

            GameEvents.BuildingRepaired += Handler;
            try
            {
                Assert.That(RepairService.TryRepairBuilding(map, pool, ap, pos), Is.True);
            }
            finally
            {
                GameEvents.BuildingRepaired -= Handler;
            }

            Assert.That(observedPos, Is.EqualTo(pos));
            Assert.That(observedHp, Is.EqualTo(10));
            Assert.That(observedMax, Is.EqualTo(10));

            void Handler(GridPos p, int hp, int maxHp)
            {
                observedPos = p;
                observedHp = hp;
                observedMax = maxHp;
            }
        }

        [Test]
        public void Repair_Base_RestoresHpAndRaisesEvent()
        {
            var (_, pool, ap) = Setup();
            var baseDefense = new Base(new GridPos(6, 6), 20);
            baseDefense.TakeDamage(6);
            var observedHp = 0;
            var observedMax = 0;
            Assert.That(RepairService.CanRepairBase(baseDefense, pool, ap), Is.True);

            GameEvents.BaseHpChanged += Handler;
            try
            {
                Assert.That(RepairService.TryRepairBase(baseDefense, pool, ap), Is.True);
            }
            finally
            {
                GameEvents.BaseHpChanged -= Handler;
            }

            Assert.That(baseDefense.CurrentHp, Is.EqualTo(20), "据点应修满");
            Assert.That(pool.GetAmount(ResourceType.RepairKit), Is.EqualTo(2));
            Assert.That(ap.Current, Is.EqualTo(9));
            Assert.That(observedHp, Is.EqualTo(20), "修满应发 BaseHpChanged");
            Assert.That(observedMax, Is.EqualTo(20));

            void Handler(int hp, int maxHp)
            {
                observedHp = hp;
                observedMax = maxHp;
            }
        }

        [Test]
        public void Repair_FullHpBase_Fails()
        {
            var (_, pool, ap) = Setup();
            var baseDefense = new Base(new GridPos(6, 6), 20);

            Assert.That(RepairService.CanRepairBase(baseDefense, pool, ap), Is.False);
            Assert.That(RepairService.TryRepairBase(baseDefense, pool, ap), Is.False);
            Assert.That(pool.GetAmount(ResourceType.RepairKit), Is.EqualTo(3));
        }

        [Test]
        public void Repair_NullArguments_Fail()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(1, 1);

            Assert.That(RepairService.CanRepairBuilding(null, pool, ap, pos), Is.False);
            Assert.That(RepairService.TryRepairBuilding(map, null, null, pos), Is.False);
            Assert.That(RepairService.CanRepairBase(null, pool, ap), Is.False);
            Assert.That(RepairService.TryRepairBase(null, pool, ap), Is.False);
        }
    }
}
