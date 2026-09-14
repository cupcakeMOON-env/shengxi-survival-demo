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
            pool.Add(ResourceType.Food, 100);
            for (var i = 0; i < workshopCount; i++)
            {
                map.Place(BuildingType.Workshop, new GridPos(5 + i, 5));
            }

            return (map, pool);
        }

        [Test]
        public void Tick_RunsBothRecipesInParallel()
        {
            var (map, pool) = Setup();

            ProductionSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(98), "两条产线各消耗 1 木头");
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.EqualTo(99), "工坊每秒应消耗 1 石头");
            Assert.That(pool.GetAmount(ResourceType.Food), Is.EqualTo(99), "修理包产线每秒消耗 1 食物");
            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(1), "每秒应合成 1 建材");
            Assert.That(pool.GetAmount(ResourceType.RepairKit), Is.EqualTo(1), "每秒应合成 1 修理包");
        }

        [Test]
        public void Tick_AccumulatesOverMultipleSeconds()
        {
            var (map, pool) = Setup();

            ProductionSystem.Tick(map, pool);
            ProductionSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(2));
            Assert.That(pool.GetAmount(ResourceType.RepairKit), Is.EqualTo(2));
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(96));
        }

        [Test]
        public void Tick_AllWorkshopsProduceIndependently()
        {
            var (map, pool) = Setup(workshopCount: 2);

            ProductionSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(2), "每座工坊各产 1");
            Assert.That(pool.GetAmount(ResourceType.RepairKit), Is.EqualTo(2), "修理包同样每座各产 1");
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(96), "两条产线 × 两座工坊共消耗 4 木头");
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.EqualTo(98));
            Assert.That(pool.GetAmount(ResourceType.Food), Is.EqualTo(98));
        }

        [Test]
        public void Tick_WithoutStone_SkipsMaterialLine_ButKeepsRepairKitLine()
        {
            var (map, pool) = Setup();
            pool.TrySpend(ResourceType.Stone, 100);

            ProductionSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Material), Is.Zero, "石头不足时建材线停");
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.Zero, "没合成就不该扣石头（本来也没有）");
            Assert.That(pool.GetAmount(ResourceType.RepairKit), Is.EqualTo(1), "修理包线不依赖石头，应照常生产");
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(99), "只被修理包线消耗 1 木头");
            Assert.That(pool.GetAmount(ResourceType.Food), Is.EqualTo(99));
        }

        [Test]
        public void Tick_WhenMaterialAtCapacity_SkipsOnlyThatLine()
        {
            var (map, pool) = Setup();
            pool.Capacity = 5;
            pool.Add(ResourceType.Material, 5);

            ProductionSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(5), "容量已满时不应超上限");
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.EqualTo(100), "建材线无法入库就不应白吃石头");
            Assert.That(pool.GetAmount(ResourceType.RepairKit), Is.EqualTo(1), "修理包线容量够，应照常生产");
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(99), "只有修理包线消耗木头");
            Assert.That(pool.GetAmount(ResourceType.Food), Is.EqualTo(99));
        }

        [Test]
        public void Tick_WhenRepairKitAtCapacity_SkipsOnlyThatLine()
        {
            var (map, pool) = Setup();
            pool.Capacity = 5;
            pool.Add(ResourceType.RepairKit, 5);

            ProductionSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.RepairKit), Is.EqualTo(5));
            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(1), "建材线容量够，应照常生产");
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(99), "只有建材线消耗木头");
            Assert.That(pool.GetAmount(ResourceType.Food), Is.EqualTo(100), "修理包线停，不消耗食物");
        }

        [Test]
        public void Catalog_WorkshopHasTwoRecipes()
        {
            var recipes = CraftingCatalog.For(BuildingType.Workshop);

            Assert.That(recipes.Count, Is.EqualTo(2), "工坊应有建材与修理包两条配方");
            Assert.That(recipes[0].Output, Is.EqualTo(ResourceType.Material));
            Assert.That(recipes[1].Output, Is.EqualTo(ResourceType.RepairKit));
            Assert.That(CraftingCatalog.For(BuildingType.ArrowTower).Count, Is.Zero, "非生产者不应有配方");
        }

        [Test]
        public void Catalog_EveryRecipeHasInputsAndPositiveOutput()
        {
            foreach (var recipe in CraftingCatalog.All)
            {
                Assert.That(recipe.OutputPerSecond, Is.GreaterThan(0), $"{recipe.Name} 产出");
                Assert.That(recipe.InputsPerSecond, Is.Not.Null.And.Not.Empty, $"{recipe.Name} 原料");
                foreach (var input in recipe.InputsPerSecond)
                {
                    Assert.That(input.AmountPerSecond, Is.GreaterThan(0), $"{recipe.Name} 原料数量");
                }
            }
        }
    }
}
