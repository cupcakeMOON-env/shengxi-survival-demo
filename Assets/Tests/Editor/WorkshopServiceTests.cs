using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    /// <summary>
    /// 工坊手动控制：每座工坊的总开关（开工 / 停产）与单条产线开关。
    /// </summary>
    public class WorkshopServiceTests
    {
        private static RecipeDef Material => CraftingCatalog.Find(BuildingType.Workshop, CraftingCatalog.IdMaterial);

        private static (GridMap map, ResourcePool pool, GridPos pos) Setup(bool withResources = true)
        {
            var map = new GridMap(10, 10);
            var pool = new ResourcePool();
            if (withResources)
            {
                pool.Add(ResourceType.Wood, 100);
                pool.Add(ResourceType.Stone, 100);
                pool.Add(ResourceType.Food, 100);
            }

            var pos = new GridPos(4, 4);
            map.Place(BuildingType.Workshop, pos);
            return (map, pool, pos);
        }

        [Test]
        public void SetProducing_StopsConsumptionAndOutput()
        {
            var (map, pool, pos) = Setup();
            Assert.That(WorkshopService.SetProducing(map, pos, false), Is.True);

            var wood = pool.GetAmount(ResourceType.Wood);
            var stone = pool.GetAmount(ResourceType.Stone);
            var food = pool.GetAmount(ResourceType.Food);

            ProductionSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(wood), "停产后不应消耗木头");
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.EqualTo(stone), "停产后不应消耗石头");
            Assert.That(pool.GetAmount(ResourceType.Food), Is.EqualTo(food), "停产后不应消耗食物");
            Assert.That(pool.GetAmount(ResourceType.Material), Is.Zero, "停产后不应产出建材");
            Assert.That(pool.GetAmount(ResourceType.RepairKit), Is.Zero, "停产后不应产出修理包");
        }

        [Test]
        public void SetProducing_OnNonWorkshopTarget_IsRejected()
        {
            var map = new GridMap(5, 5);
            map.Place(BuildingType.Wall, new GridPos(1, 1));

            Assert.That(WorkshopService.IsWorkshop(map, new GridPos(1, 1)), Is.False);
            Assert.That(WorkshopService.SetProducing(map, new GridPos(1, 1), false), Is.False, "围墙不是工坊");
            Assert.That(WorkshopService.SetProducing(map, new GridPos(4, 4), false), Is.False, "空格子不是工坊");
        }

        [Test]
        public void SetProducing_OnlyRaisesEventWhenStateActuallyChanges()
        {
            var (map, _, pos) = Setup();
            var count = 0;
            System.Action<GridPos> handler = _ => count++;
            GameEvents.WorkshopStateChanged += handler;

            try
            {
                WorkshopService.SetProducing(map, pos, true); // 本来就开工
                Assert.That(count, Is.Zero, "状态没变不该发事件");

                WorkshopService.SetProducing(map, pos, false);
                Assert.That(count, Is.EqualTo(1));

                WorkshopService.SetProducing(map, pos, false);
                Assert.That(count, Is.EqualTo(1), "重复设置同一状态不该再发事件");
            }
            finally
            {
                GameEvents.WorkshopStateChanged -= handler;
            }
        }

        [Test]
        public void SetRecipeEnabled_StopsOnlyThatLine()
        {
            var (map, pool, pos) = Setup();

            Assert.That(
                WorkshopService.SetRecipeEnabled(map, pos, CraftingCatalog.IdRepairKit, false),
                Is.True);

            ProductionSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(1), "建材线应照常生产");
            Assert.That(pool.GetAmount(ResourceType.RepairKit), Is.Zero, "修理包线已关闭");
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(99), "只有建材线消耗木头");
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.EqualTo(99));
            Assert.That(pool.GetAmount(ResourceType.Food), Is.EqualTo(100), "修理包线停了就不该再吃食物");
        }

        [Test]
        public void SetRecipeEnabled_UnknownRecipe_IsRejected()
        {
            var (map, _, pos) = Setup();

            Assert.That(WorkshopService.SetRecipeEnabled(map, pos, "not-a-recipe", false), Is.False);
            Assert.That(WorkshopService.SetRecipeEnabled(map, pos, null, false), Is.False);
            Assert.That(map.GetTile(pos).IsRecipeEnabled("not-a-recipe"), Is.True, "非法 Id 不应留下痕迹");
        }

        [Test]
        public void IsEffectivelyProducing_FalseWhenStoppedOrAllLinesOff()
        {
            var (map, _, pos) = Setup();
            var tile = map.GetTile(pos);

            Assert.That(WorkshopService.IsEffectivelyProducing(tile), Is.True, "默认开工且两条线都开");

            WorkshopService.SetRecipeEnabled(map, pos, CraftingCatalog.IdMaterial, false);
            Assert.That(WorkshopService.IsEffectivelyProducing(tile), Is.True, "还有一条线在跑");

            WorkshopService.SetRecipeEnabled(map, pos, CraftingCatalog.IdRepairKit, false);
            Assert.That(WorkshopService.IsEffectivelyProducing(tile), Is.False, "所有产线都关掉了");

            WorkshopService.SetRecipeEnabled(map, pos, CraftingCatalog.IdMaterial, true);
            Assert.That(WorkshopService.IsEffectivelyProducing(tile), Is.True);

            WorkshopService.SetProducing(map, pos, false);
            Assert.That(WorkshopService.IsEffectivelyProducing(tile), Is.False, "停产优先");
        }

        [Test]
        public void ConfiguredRates_ReflectsEnabledLines()
        {
            var (map, _, pos) = Setup();
            var tile = map.GetTile(pos);

            var rates = WorkshopService.ConfiguredRates(tile);
            Assert.That(rates[(int)ResourceType.Wood], Is.EqualTo(-2), "两条线各吃 1 木头");
            Assert.That(rates[(int)ResourceType.Stone], Is.EqualTo(-1));
            Assert.That(rates[(int)ResourceType.Food], Is.EqualTo(-1));
            Assert.That(rates[(int)ResourceType.Material], Is.EqualTo(1));
            Assert.That(rates[(int)ResourceType.RepairKit], Is.EqualTo(1));

            WorkshopService.SetRecipeEnabled(map, pos, CraftingCatalog.IdRepairKit, false);
            rates = WorkshopService.ConfiguredRates(tile);
            Assert.That(rates[(int)ResourceType.Wood], Is.EqualTo(-1), "关掉修理包线后木头只要 1");
            Assert.That(rates[(int)ResourceType.Food], Is.Zero);
            Assert.That(rates[(int)ResourceType.RepairKit], Is.Zero);
            Assert.That(rates[(int)ResourceType.Material], Is.EqualTo(1));

            WorkshopService.SetProducing(map, pos, false);
            rates = WorkshopService.ConfiguredRates(tile);
            Assert.That(rates[(int)ResourceType.Wood], Is.Zero, "停产后没有收支");
            Assert.That(rates[(int)ResourceType.Material], Is.Zero);
        }

        [Test]
        public void EvaluateRecipe_ReportsWhyALineIsNotRunning()
        {
            var (map, pool, pos) = Setup(withResources: false);
            var tile = map.GetTile(pos);

            Assert.That(
                ProductionSystem.EvaluateRecipe(pool, tile, Material),
                Is.EqualTo(RecipeState.MissingInputs),
                "没原料");

            WorkshopService.SetProducing(map, pos, false);
            Assert.That(
                ProductionSystem.EvaluateRecipe(pool, tile, Material),
                Is.EqualTo(RecipeState.WorkshopStopped),
                "停产优先于缺原料");

            WorkshopService.SetProducing(map, pos, true);
            WorkshopService.SetRecipeEnabled(map, pos, CraftingCatalog.IdMaterial, false);
            Assert.That(
                ProductionSystem.EvaluateRecipe(pool, tile, Material),
                Is.EqualTo(RecipeState.Disabled),
                "单条产线关闭");

            WorkshopService.SetRecipeEnabled(map, pos, CraftingCatalog.IdMaterial, true);
            pool.Add(ResourceType.Wood, 11);
            pool.Add(ResourceType.Stone, 11);
            pool.Capacity = 5;
            pool.Add(ResourceType.Material, 5);
            Assert.That(
                ProductionSystem.EvaluateRecipe(pool, tile, Material),
                Is.EqualTo(RecipeState.OutputFull),
                "建材满仓");
        }

        [Test]
        public void TwoWorkshops_AreControlledIndependently()
        {
            var map = new GridMap(10, 10);
            var pool = new ResourcePool();
            pool.Add(ResourceType.Wood, 100);
            pool.Add(ResourceType.Stone, 100);
            pool.Add(ResourceType.Food, 100);

            var a = new GridPos(2, 2);
            var b = new GridPos(6, 6);
            map.Place(BuildingType.Workshop, a);
            map.Place(BuildingType.Workshop, b);

            // 只停 A，并单独关掉 A 的修理包线
            WorkshopService.SetProducing(map, a, false);
            WorkshopService.SetRecipeEnabled(map, a, CraftingCatalog.IdRepairKit, false);

            Assert.That(map.GetTile(a).ProductionEnabled, Is.False);
            Assert.That(map.GetTile(b).ProductionEnabled, Is.True, "停 A 不应影响 B 的开工状态");
            Assert.That(
                map.GetTile(b).IsRecipeEnabled(CraftingCatalog.IdRepairKit),
                Is.True,
                "关 A 的产线不应影响 B 的同名产线");
            Assert.That(map.GetTile(a).IsRecipeEnabled(CraftingCatalog.IdMaterial), Is.True, "A 的建材线仍开着");

            ProductionSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(1), "只有 B 在产，建材应为 1 而不是 2");
            Assert.That(pool.GetAmount(ResourceType.RepairKit), Is.EqualTo(1), "修理包只有 B 在产");
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(98), "只有 B 的两条线吃木头");
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.EqualTo(99));
            Assert.That(pool.GetAmount(ResourceType.Food), Is.EqualTo(99));

            // 反过来：A 复工（修理包线仍是关的）、B 停产
            WorkshopService.SetProducing(map, a, true);
            WorkshopService.SetProducing(map, b, false);

            ProductionSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Material), Is.EqualTo(2), "这一轮轮到 A 产建材");
            Assert.That(pool.GetAmount(ResourceType.RepairKit), Is.EqualTo(1), "A 的修理包线是关的，不应再产");
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(97), "A 只有建材线在吃木头");
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.EqualTo(98));
            Assert.That(pool.GetAmount(ResourceType.Food), Is.EqualTo(99), "两轮下来都没人吃食物");
        }

        [Test]
        public void DemolishThenRebuild_ResetsProductionSwitches()
        {
            var (map, _, pos) = Setup();
            WorkshopService.SetProducing(map, pos, false);
            WorkshopService.SetRecipeEnabled(map, pos, CraftingCatalog.IdMaterial, false);

            map.RemoveBuilding(pos);
            Assert.That(map.Place(BuildingType.Workshop, pos), Is.True);

            var tile = map.GetTile(pos);
            Assert.That(tile.ProductionEnabled, Is.True, "新建的工坊应默认开工，不继承拆掉那座的状态");
            Assert.That(tile.IsRecipeEnabled(CraftingCatalog.IdMaterial), Is.True, "新建的工坊应默认产线全开");
        }
    }
}
