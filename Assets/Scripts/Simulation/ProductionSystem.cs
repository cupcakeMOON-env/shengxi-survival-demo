namespace ShengXi.Simulation
{
    /// <summary>
    /// 工坊自动生产：每 tick 扫描地图，让每座工坊按配方把仓库里的
    /// 木头/石头合成为建材（产出同样受仓库容量约束，满了就暂停不消耗原料）。
    /// 只白天运转（由 GameLoop 只在白天调 Tick），与采集站共用 1 秒口径。
    /// </summary>
    public static class ProductionSystem
    {
        public const float TickInterval = 1f;

        public static void Tick(GridMap map, ResourcePool pool)
        {
            if (map == null || pool == null)
            {
                return;
            }

            for (var x = 0; x < map.Width; x++)
            {
                for (var y = 0; y < map.Height; y++)
                {
                    var pos = new GridPos(x, y);
                    var tile = map.GetTile(pos);
                    if (tile == null || tile.Building != BuildingType.Workshop)
                    {
                        continue;
                    }

                    var recipe = CraftingCatalog.For(BuildingType.Workshop);
                    if (recipe == null ||
                        recipe.OutputPerSecond <= 0 ||
                        !HasInputs(pool, recipe) ||
                        !HasOutputSpace(pool, recipe))
                    {
                        continue;
                    }

                    pool.TrySpend(ResourceType.Wood, recipe.WoodPerSecond);
                    pool.TrySpend(ResourceType.Stone, recipe.StonePerSecond);
                    pool.TrySpend(ResourceType.Food, recipe.FoodPerSecond);
                    pool.Add(recipe.Output, recipe.OutputPerSecond);
                }
            }
        }

        private static bool HasInputs(ResourcePool pool, RecipeDef recipe) =>
            pool.CanSpend(ResourceType.Wood, recipe.WoodPerSecond) &&
            pool.CanSpend(ResourceType.Stone, recipe.StonePerSecond) &&
            pool.CanSpend(ResourceType.Food, recipe.FoodPerSecond);

        /// <summary>容量差判断：产出不会因为容量截断而「白吃原料」。 </summary>
        private static bool HasOutputSpace(ResourcePool pool, RecipeDef recipe) =>
            pool.Capacity - pool.GetAmount(recipe.Output) >= recipe.OutputPerSecond;
    }
}
