using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class DemolishServiceTests
    {
        private static (GridMap map, ResourcePool pool, ActionPointSystem ap) Setup()
        {
            var map = new GridMap(10, 10);
            var pool = new ResourcePool();
            pool.Add(ResourceType.Wood, 100);
            pool.Add(ResourceType.Stone, 100);
            return (map, pool, new ActionPointSystem(10));
        }

        [Test]
        public void Demolish_EmptyTile_Fails()
        {
            var (map, pool, ap) = Setup();

            var ok = DemolishService.TryDemolish(map, pool, new GridPos(0, 0));

            Assert.That(ok, Is.False);
            Assert.That(ap.Current, Is.EqualTo(10), "失败不应扣行动点");
        }

        [Test]
        public void Demolish_OutOfBounds_Fails()
        {
            var (map, pool, _) = Setup();

            Assert.That(DemolishService.CanDemolish(map, new GridPos(-1, -1)), Is.False);
            Assert.That(DemolishService.TryDemolish(map, pool, new GridPos(99, 99)), Is.False);
        }

        [Test]
        public void Demolish_BaseTile_Fails()
        {
            var (map, pool, _) = Setup();
            map.Place(BuildingType.Base, new GridPos(1, 1));

            Assert.That(DemolishService.CanDemolish(map, new GridPos(1, 1)), Is.False);
            Assert.That(DemolishService.TryDemolish(map, pool, new GridPos(1, 1)), Is.False);
            Assert.That(map.GetTile(new GridPos(1, 1)).Building, Is.EqualTo(BuildingType.Base));
        }

        [Test]
        public void Demolish_WorksWithZeroActionPoints()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(2, 2);
            map.Place(BuildingType.Wall, pos);
            ap.Spend(10);

            Assert.That(DemolishService.CanDemolish(map, pos), Is.True, "拆除不应依赖行动点");
            Assert.That(DemolishService.TryDemolish(map, pool, pos), Is.True);
            Assert.That(map.GetTile(pos).Building, Is.EqualTo(BuildingType.None));
            Assert.That(ap.Current, Is.Zero, "拆除不应扣行动点");
        }

        [Test]
        public void Demolish_Wall_FreesTileRefundsHalf()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(3, 3);
            Assert.That(BuildService.TryBuild(map, pool, ap, BuildingCatalog.Get(BuildingType.Wall), pos), Is.True);
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(98));
            Assert.That(ap.Current, Is.EqualTo(9));

            Assert.That(DemolishService.TryDemolish(map, pool, pos), Is.True);

            Assert.That(map.GetTile(pos).Building, Is.EqualTo(BuildingType.None));
            Assert.That(map.GetTile(pos).IsWalkable, Is.True, "拆除围墙后应恢复可通行");
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(99), "围墙木2 应返还 50% = 1");
            Assert.That(ap.Current, Is.EqualTo(9), "拆除不消耗行动点，只算建造的 1 点");
        }

        [Test]
        public void Demolish_UpgradedWall_RefundsHalfUpgradeMaterial()
        {
            var (map, pool, ap) = Setup();
            pool.Add(ResourceType.Material, 20);
            var pos = new GridPos(3, 3);
            Assert.That(BuildService.TryBuild(map, pool, ap, BuildingCatalog.Get(BuildingType.Wall), pos), Is.True);
            Assert.That(UpgradeService.TryUpgrade(map, pool, ap, pos), Is.True, "应先能升到 2 级");
            Assert.That(map.GetTile(pos).BuildingLevel, Is.EqualTo(2));
            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(16), "围墙升级消耗 4 建材");

            Assert.That(DemolishService.TryDemolish(map, pool, pos), Is.True);

            Assert.That(map.GetTile(pos).Building, Is.EqualTo(BuildingType.None));
            Assert.That(map.GetTile(pos).BuildingLevel, Is.Zero, "拆除后等级应清零");
            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(18), "投入的 4 建材应返还 50% = 2");
        }

        [Test]
        public void Demolish_Warehouse_ReducesCapacityAndRefundsHalf()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(0, 0);
            Assert.That(BuildService.TryBuild(map, pool, ap, BuildingCatalog.Get(BuildingType.Warehouse), pos), Is.True);
            Assert.That(pool.Capacity, Is.EqualTo(200));
            var woodBefore = pool.GetAmount(ResourceType.Wood);
            var stoneBefore = pool.GetAmount(ResourceType.Stone);

            Assert.That(DemolishService.TryDemolish(map, pool, pos), Is.True);

            Assert.That(map.GetTile(pos).Building, Is.EqualTo(BuildingType.None));
            Assert.That(pool.Capacity, Is.EqualTo(ResourcePool.DefaultCapacity));
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(woodBefore + 5), "木10 应返还 5");
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.EqualTo(stoneBefore + 3), "石5 的 50% = 2.5，向上取整返 3");
        }

        [Test]
        public void Demolish_Warehouse_ClampsResourcesToNewCapacity()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(0, 0);
            Assert.That(BuildService.TryBuild(map, pool, ap, BuildingCatalog.Get(BuildingType.Warehouse), pos), Is.True);
            pool.Add(ResourceType.Wood, 80); // 容量 200：90 + 80 → 170
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(170));

            Assert.That(DemolishService.TryDemolish(map, pool, pos), Is.True);

            Assert.That(pool.Capacity, Is.EqualTo(100));
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(100), "超出新容量的资源应被截断");
        }

        [Test]
        public void Demolish_Collector_StopsAutomaticCollection()
        {
            var (map, pool, ap) = Setup();
            var forest = new GridPos(2, 2);
            map.SetTerrain(forest, TerrainType.Forest, 10);
            Assert.That(BuildService.TryBuild(map, pool, ap, BuildingCatalog.Get(BuildingType.Collector), forest), Is.True);
            CollectorSystem.Tick(map, pool);
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(96), "100-5 造价 + 1 采集");
            var resourceLeft = map.GetTile(forest).ResourceAmount;

            Assert.That(DemolishService.TryDemolish(map, pool, forest), Is.True);
            var woodAfterDemolish = pool.GetAmount(ResourceType.Wood);
            CollectorSystem.Tick(map, pool);

            Assert.That(map.GetTile(forest).Building, Is.EqualTo(BuildingType.None));
            Assert.That(map.GetTile(forest).ResourceAmount, Is.EqualTo(resourceLeft), "拆除不应消耗资源格余量");
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(woodAfterDemolish), "拆除后采集站不应再产出");
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(99), "96 + 木5 的 50% 向上取整 = 3");
        }

        [Test]
        public void Demolish_RaisesBuildingRemovedEvent()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(4, 4);
            map.Place(BuildingType.Workshop, pos);
            GridPos? removed = null;
            GameEvents.BuildingRemoved += Handler;
            try
            {
                Assert.That(DemolishService.TryDemolish(map, pool, pos), Is.True);
                Assert.That(removed, Is.EqualTo(pos));
            }
            finally
            {
                GameEvents.BuildingRemoved -= Handler;
            }

            void Handler(GridPos p) => removed = p;
        }

        [Test]
        public void Demolish_NullArguments_Fails()
        {
            var (map, pool, _) = Setup();

            Assert.That(DemolishService.TryDemolish(null, pool, new GridPos(0, 0)), Is.False);
            Assert.That(DemolishService.TryDemolish(map, null, new GridPos(0, 0)), Is.False);
            Assert.That(DemolishService.CanDemolish(null, new GridPos(0, 0)), Is.False);
        }

        [Test]
        public void Refund_RoundsUp_SoNoFractionIsLost()
        {
            // 50% 向上取整：偶数造价与截断结果一致，奇数造价多返 1
            Assert.That(DemolishService.CeilPercent(1, 50), Is.EqualTo(1));
            Assert.That(DemolishService.CeilPercent(2, 50), Is.EqualTo(1));
            Assert.That(DemolishService.CeilPercent(3, 50), Is.EqualTo(2));
            Assert.That(DemolishService.CeilPercent(4, 50), Is.EqualTo(2));
            Assert.That(DemolishService.CeilPercent(5, 50), Is.EqualTo(3), "采集站木 5 应返 3");
            Assert.That(DemolishService.CeilPercent(6, 50), Is.EqualTo(3));
            Assert.That(DemolishService.CeilPercent(8, 50), Is.EqualTo(4));
            Assert.That(DemolishService.CeilPercent(10, 50), Is.EqualTo(5));
            Assert.That(DemolishService.CeilPercent(12, 50), Is.EqualTo(6));
            Assert.That(DemolishService.CeilPercent(15, 50), Is.EqualTo(8), "箭塔木 15 应返 8");

            Assert.That(DemolishService.CeilPercent(0, 50), Is.Zero, "没投入就没得返");
            Assert.That(DemolishService.CeilPercent(5, 0), Is.Zero, "比例非法时不返还");
        }

        [Test]
        public void Demolish_ArrowTower_RefundsOddCostRoundedUp()
        {
            var (map, pool, ap) = Setup();
            var pos = new GridPos(3, 3);
            Assert.That(
                BuildService.TryBuild(map, pool, ap, BuildingCatalog.Get(BuildingType.ArrowTower), pos),
                Is.True);
            var woodBefore = pool.GetAmount(ResourceType.Wood);
            var stoneBefore = pool.GetAmount(ResourceType.Stone);

            Assert.That(DemolishService.TryDemolish(map, pool, pos), Is.True);

            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(woodBefore + 8), "木15 的 50% = 7.5 → 返 8");
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.EqualTo(stoneBefore + 5), "石10 的 50% = 5");
        }
    }
}
