using System.Collections.Generic;
using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class FoodSupplySystemTests
    {
        private static (GridMap map, ResourcePool pool) Setup(int towerCount = 1)
        {
            var map = new GridMap(12, 12);
            var pool = new ResourcePool();
            for (var i = 0; i < towerCount; i++)
            {
                map.Place(BuildingType.ArrowTower, new GridPos(3 + i, 3));
            }

            return (map, pool);
        }

        [Test]
        public void Tick_WithEnoughFood_SpendsFoodAndKeepsTowerWorking()
        {
            var (map, pool) = Setup();
            pool.Add(ResourceType.Food, 1);
            var towerPos = new GridPos(3, 3);

            FoodSupplySystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Food), Is.Zero, "箭塔每秒应消耗 1 食物");
            Assert.That(FoodSupplySystem.IsSupplied(map, towerPos), Is.True);
            Assert.That(map.GetTile(towerPos).BuildingStarved, Is.False);
        }

        [Test]
        public void Tick_WithoutFood_StarvesTowerAndDoesNotSpend()
        {
            var (map, pool) = Setup();
            var towerPos = new GridPos(3, 3);

            FoodSupplySystem.Tick(map, pool);

            Assert.That(FoodSupplySystem.IsSupplied(map, towerPos), Is.False, "没粮的箭塔应断粮");
            Assert.That(map.GetTile(towerPos).BuildingStarved, Is.True);
            Assert.That(pool.GetAmount(ResourceType.Food), Is.Zero, "断粮结算不应扣费");
        }

        [Test]
        public void Tick_AfterFoodRestored_TowerResumesSupply()
        {
            var (map, pool) = Setup();
            var towerPos = new GridPos(3, 3);
            FoodSupplySystem.Tick(map, pool);
            Assert.That(FoodSupplySystem.IsSupplied(map, towerPos), Is.False);

            pool.Add(ResourceType.Food, 1);
            FoodSupplySystem.Tick(map, pool);

            Assert.That(FoodSupplySystem.IsSupplied(map, towerPos), Is.True, "食物恢复后下个结算应复工");
            Assert.That(pool.GetAmount(ResourceType.Food), Is.Zero, "复工结算应正常扣费");
        }

        [Test]
        public void DemandIsCombined_AllTowersShareTheSameSupplyState()
        {
            var (map, pool) = Setup(towerCount: 2);
            var first = new GridPos(3, 3);
            var second = new GridPos(4, 3);

            pool.Add(ResourceType.Food, 1);
            FoodSupplySystem.Tick(map, pool);

            Assert.That(FoodSupplySystem.IsSupplied(map, first), Is.False, "1 食物不够 2 座塔，第一座也应断粮");
            Assert.That(FoodSupplySystem.IsSupplied(map, second), Is.False, "第二座塔同样断粮");
            Assert.That(pool.GetAmount(ResourceType.Food), Is.EqualTo(1), "不足时不扣费，食物留给恢复时刻");

            pool.Add(ResourceType.Food, 1);
            FoodSupplySystem.Tick(map, pool);

            Assert.That(FoodSupplySystem.IsSupplied(map, first), Is.True, "凑够总需求后两座塔一起复工");
            Assert.That(FoodSupplySystem.IsSupplied(map, second), Is.True);
            Assert.That(pool.GetAmount(ResourceType.Food), Is.Zero, "两座塔每秒应共消耗 2 食物");
        }

        [Test]
        public void Refresh_ReflectsAffordabilityWithoutCharging()
        {
            var (map, pool) = Setup();
            var towerPos = new GridPos(3, 3);
            pool.Add(ResourceType.Food, 1);

            FoodSupplySystem.Refresh(map, pool);

            Assert.That(FoodSupplySystem.IsSupplied(map, towerPos), Is.True);
            Assert.That(pool.GetAmount(ResourceType.Food), Is.EqualTo(1), "夜晚开场的刷新只判定不扣费");

            FoodSupplySystem.RestoreAll(map);
            Assert.That(FoodSupplySystem.IsSupplied(map, towerPos), Is.True);
        }

        [Test]
        public void SupplyChangedEvent_RaisedOnlyOnStateTransition()
        {
            var (map, pool) = Setup();
            var events = new List<bool>();
            GameEvents.BuildingSupplyChanged += Handler;
            try
            {
                FoodSupplySystem.Tick(map, pool);
                FoodSupplySystem.Tick(map, pool);
                pool.Add(ResourceType.Food, 1);
                FoodSupplySystem.Tick(map, pool);
            }
            finally
            {
                GameEvents.BuildingSupplyChanged -= Handler;
            }

            Assert.That(events, Is.EqualTo(new List<bool> { false, true }), "只在断粮/恢复切换时发事件");

            void Handler(GridPos pos, bool supplied) => events.Add(supplied);
        }

        [Test]
        public void RestoreAll_AfterNightEnds_UnstarvesTowers()
        {
            var (map, pool) = Setup();
            var towerPos = new GridPos(3, 3);
            FoodSupplySystem.Tick(map, pool);
            Assert.That(map.GetTile(towerPos).BuildingStarved, Is.True);

            FoodSupplySystem.RestoreAll(map);

            Assert.That(map.GetTile(towerPos).BuildingStarved, Is.False, "白天不消耗食物，塔应恢复待命状态");
            Assert.That(FoodSupplySystem.IsSupplied(map, towerPos), Is.True);
        }
    }
}
