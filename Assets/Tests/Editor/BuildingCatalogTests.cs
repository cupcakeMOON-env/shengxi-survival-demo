using System.Linq;
using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class BuildingCatalogTests
    {
        [Test]
        public void AllBuildingTypesAreUnique()
        {
            var types = BuildingCatalog.All.Select(d => d.Type).ToList();

            Assert.That(types.Distinct().Count(), Is.EqualTo(types.Count));
        }

        [Test]
        public void EveryDef_HasNameAndValidCosts()
        {
            foreach (var def in BuildingCatalog.All)
            {
                Assert.That(string.IsNullOrEmpty(def.Name), Is.False, $"{def.Type} name");
                Assert.That(def.WoodCost, Is.GreaterThanOrEqualTo(0), $"{def.Type} wood");
                Assert.That(def.StoneCost, Is.GreaterThanOrEqualTo(0), $"{def.Type} stone");
                Assert.That(def.FoodCost, Is.GreaterThanOrEqualTo(0), $"{def.Type} food");
                Assert.That(def.ActionPointCost, Is.GreaterThan(0), $"{def.Type} AP");
            }
        }

        [Test]
        public void EveryDef_HasPositiveMaxHp()
        {
            foreach (var def in BuildingCatalog.All)
            {
                Assert.That(def.MaxHp, Is.GreaterThan(0), $"{def.Type} MaxHp");
            }
        }

        [Test]
        public void Wall_IsTheSturdiestBuilding()
        {
            var wall = BuildingCatalog.Get(BuildingType.Wall);

            foreach (var def in BuildingCatalog.All)
            {
                if (def.Type == BuildingType.Wall)
                {
                    continue;
                }

                Assert.That(
                    wall.MaxHp,
                    Is.GreaterThan(def.MaxHp),
                    $"围墙应比其他建筑更厚（{def.Type} MaxHp={def.MaxHp}）");
            }
        }

        [Test]
        public void Get_ReturnsDefForType()
        {
            Assert.That(BuildingCatalog.Get(BuildingType.Warehouse).Name, Is.EqualTo("仓库"));
            Assert.That(BuildingCatalog.Get(BuildingType.Collector).RequiresResourceTile, Is.True);
            Assert.That(BuildingCatalog.Get(BuildingType.ArrowTower), Is.Not.Null);
        }

        [Test]
        public void ArrowTower_HasDataDrivenRangeAndDamage()
        {
            var tower = BuildingCatalog.Get(BuildingType.ArrowTower);

            Assert.That(tower.Range, Is.EqualTo(3));
            Assert.That(tower.Damage, Is.EqualTo(1));
            Assert.That(tower.FoodPerSecond, Is.EqualTo(1), "箭塔夜晚每秒应消耗 1 食物");
            Assert.That(BuildingCatalog.Get(BuildingType.Warehouse).Range, Is.Zero);
            Assert.That(BuildingCatalog.Get(BuildingType.Wall).Damage, Is.Zero);
        }

        [Test]
        public void OnlyArrowTower_ConsumesNightFood()
        {
            foreach (var def in BuildingCatalog.All)
            {
                var expected = def.Type == BuildingType.ArrowTower ? 1 : 0;
                Assert.That(
                    def.FoodPerSecond,
                    Is.EqualTo(expected),
                    $"{def.Type} 的 FoodPerSecond 应为 {expected}（当前只有箭塔会消耗食物）");
            }
        }

        [Test]
        public void Collector_CostsWoodOnly()
        {
            var collector = BuildingCatalog.Get(BuildingType.Collector);

            Assert.That(collector.WoodCost, Is.GreaterThan(0), "采集站仍然要花木材");
            Assert.That(
                collector.StoneCost,
                Is.Zero,
                "采集站是唯一的资源来源，不能要石头：否则石头花光后就再也造不出采集站，整局卡死");
            Assert.That(collector.FoodCost, Is.Zero, "采集站不能要食物，食物同样只能靠采集站产出");
        }
    }
}
