using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    /// <summary>结算数据快照：资源、据点、建筑明细与结局。</summary>
    public class SettlementServiceTests
    {
        private static GridMap BuildMap(out ResourcePool pool)
        {
            var map = new GridMap(10, 10);
            map.Place(BuildingType.Base, new GridPos(5, 5));
            map.Place(BuildingType.Collector, new GridPos(1, 1));
            map.Place(BuildingType.Workshop, new GridPos(2, 2));
            map.Place(BuildingType.ArrowTower, new GridPos(3, 3));
            map.Place(BuildingType.ArrowTower, new GridPos(4, 3));

            pool = new ResourcePool { Capacity = 200 };
            pool.Add(ResourceType.Wood, 24);
            pool.Add(ResourceType.Stone, 12);
            pool.Add(ResourceType.Food, 8);
            pool.Add(ResourceType.Material, 3);
            pool.Add(ResourceType.RepairKit, 1);
            return map;
        }

        [Test]
        public void Build_SnapshotsResourcesBaseAndDay()
        {
            var map = BuildMap(out var pool);
            var actionPoints = new ActionPointSystem(10);
            actionPoints.Spend(6);
            var cycle = new DayCycle();
            cycle.StartNight();
            var baseDefense = new Base(new GridPos(5, 5), 20);
            baseDefense.TakeDamage(6);

            var summary = SettlementService.Build(map, pool, actionPoints, cycle, baseDefense, RunOutcome.Defeat);

            Assert.That(summary.Outcome, Is.EqualTo(RunOutcome.Defeat));
            Assert.That(summary.Day, Is.EqualTo(1));
            Assert.That(summary.IsNight, Is.True);
            Assert.That(summary.BaseHp, Is.EqualTo(14));
            Assert.That(summary.BaseMaxHp, Is.EqualTo(20));
            Assert.That(summary.ActionPoints, Is.EqualTo(4));
            Assert.That(summary.Capacity, Is.EqualTo(200));
            Assert.That(summary.Wood, Is.EqualTo(24));
            Assert.That(summary.Stone, Is.EqualTo(12));
            Assert.That(summary.Food, Is.EqualTo(8));
            Assert.That(summary.Material, Is.EqualTo(3));
            Assert.That(summary.RepairKit, Is.EqualTo(1));
        }

        [Test]
        public void Build_CountsBuildingsByType_AndSkipsBase()
        {
            var map = BuildMap(out var pool);

            var summary = SettlementService.Build(
                map, pool, new ActionPointSystem(10), new DayCycle(),
                new Base(new GridPos(5, 5), 20), RunOutcome.Settled);

            Assert.That(summary.TotalBuildings, Is.EqualTo(4), "据点不计入建筑数");
            Assert.That(summary.Buildings.Count, Is.EqualTo(3), "三种建筑各一条明细");
            Assert.That(summary.Buildings[0].Type, Is.EqualTo(BuildingType.Collector), "按枚举顺序，采集站在最前");
            Assert.That(summary.Buildings[0].Count, Is.EqualTo(1));
            Assert.That(summary.Buildings[1].Type, Is.EqualTo(BuildingType.ArrowTower));
            Assert.That(summary.Buildings[1].Count, Is.EqualTo(2), "同类建筑合并成一条");
            Assert.That(summary.Buildings[2].Type, Is.EqualTo(BuildingType.Workshop));
        }

        [Test]
        public void DescribeBuildings_ListsCountsInEnumOrder()
        {
            var map = BuildMap(out var pool);
            var summary = SettlementService.Build(
                map, pool, new ActionPointSystem(10), new DayCycle(),
                new Base(new GridPos(5, 5), 20), RunOutcome.Settled);

            Assert.That(SettlementService.DescribeBuildings(summary), Is.EqualTo("采集站 1 · 箭塔 2 · 工坊 1"));
        }

        [Test]
        public void Build_IgnoresBuildingsAtZeroHp()
        {
            var map = BuildMap(out var pool);
            map.SetBuildingHp(new GridPos(4, 3), 0); // 一座箭塔被打到 0 血，尚未清理

            var summary = SettlementService.Build(
                map, pool, new ActionPointSystem(10), new DayCycle(),
                new Base(new GridPos(5, 5), 20), RunOutcome.Settled);

            var towers = summary.Buildings.Find(b => b.Type == BuildingType.ArrowTower);
            Assert.That(towers, Is.Not.Null);
            Assert.That(towers.Count, Is.EqualTo(1), "0 血的建筑不该算进结算");
            Assert.That(summary.TotalBuildings, Is.EqualTo(3));
        }

        [Test]
        public void DescribeBuildings_WithoutBuildings_SaysNone()
        {
            Assert.That(SettlementService.DescribeBuildings(new RunSummary()), Is.EqualTo("无"));
            Assert.That(SettlementService.DescribeBuildings(null), Is.EqualTo("无"));
        }

        [Test]
        public void Build_ToleratesEmptyState()
        {
            var summary = SettlementService.Build(null, null, null, null, null, RunOutcome.Settled);

            Assert.That(summary.Outcome, Is.EqualTo(RunOutcome.Settled));
            Assert.That(summary.Day, Is.Zero);
            Assert.That(summary.TotalBuildings, Is.Zero);
            Assert.That(summary.BaseMaxHp, Is.Zero);
        }
    }
}
