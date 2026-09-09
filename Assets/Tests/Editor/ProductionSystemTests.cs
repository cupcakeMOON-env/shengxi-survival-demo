using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class ProductionSystemTests
    {
        private static (GridMap map, ResourcePool pool) Setup(int workshopCount = 1)
        {
            var map = new GridMap(20, 20);
            var pool = new ResourcePool();
            pool.Add(ResourceType.Wood, 100);
            pool.Add(ResourceType.Stone, 100);
            for (var i = 0; i < workshopCount; i++)
            {
                map.Place(BuildingType.Workshop, new GridPos(5 + i, 5));
            }

            return (map, pool);
        }

        [Test]
        public void Tick_ConsumesWoodAndStone_ProducesMaterial()
        {
            var (map, pool) = Setup();

            ProductionSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(99), "工坊每秒应消耗 1 木头");
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.EqualTo(99), "工坊每秒应消耗 1 石头");
            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(1), "每秒应合成 1 建材");
        }

        [Test]
        public void Tick_AccumulatesOverMultipleSeconds()
        {
            var (map, pool) = Setup();

            ProductionSystem.Tick(map, pool);
            ProductionSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(2));
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(98));
        }

        [Test]
        public void Tick_AllWorkshopsProduceIndependently()
        {
            var (map, pool) = Setup(workshopCount: 2);

            ProductionSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(2), "每座工坊各产 1");
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(98), "两座工坊各消耗 1 木头");
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.EqualTo(98));
        }

        [Test]
        public void Tick_WithoutInputs_ProducesNothing()
        {
            var (map, pool) = Setup();
            pool.TrySpend(ResourceType.Stone, 100);

            ProductionSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Material), Is.Zero, "石头不足不应合成");
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(100), "没有合成就不应消耗木头");
        }

        [Test]
        public void Tick_WhenMaterialAtCapacity_DoesNotConsumeInputs()
        {
            var (map, pool) = Setup();
            pool.Capacity = 5;
            pool.Add(ResourceType.Material, 5);
            var woodBefore = pool.GetAmount(ResourceType.Wood);
            var stoneBefore = pool.GetAmount(ResourceType.Stone);

            ProductionSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(5), "容量已满时不应超上限");
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(woodBefore), "无法入库就不应白吃原料");
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.EqualTo(stoneBefore));
        }

        [Test]
        public void Catalog_OnlyWorkshopHasRecipe()
        {
            Assert.That(CraftingCatalog.For(BuildingType.Workshop), Is.Not.Null);
            Assert.That(CraftingCatalog.For(BuildingType.ArrowTower), Is.Null, "非生产者不应有配方");
        }
    }
}
