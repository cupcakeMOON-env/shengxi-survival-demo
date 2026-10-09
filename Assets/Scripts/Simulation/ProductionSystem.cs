namespace ShengXi.Simulation
{
    /// <summary>
    /// 工坊单条产线此刻的状态。UI 展示与结算共用这一套判定，
    /// 保证界面说的「缺原料」就是结算时真正跳过的原因。
    /// </summary>
    public enum RecipeState
    {
        /// <summary>原料够、产物有空间，本 tick 会正常产出。</summary>
        Producing = 0,

        /// <summary>整座工坊被玩家停产。</summary>
        WorkshopStopped = 1,

        /// <summary>这条产线被玩家单独关闭。</summary>
        Disabled = 2,

        /// <summary>原料不足。</summary>
        MissingInputs = 3,

        /// <summary>产物已达容量上限。</summary>
        OutputFull = 4,
    }

    /// <summary>
    /// 工坊自动生产：每 tick 扫描地图，让每座工坊按配方把仓库里的
    /// 原料合成为产物（产出同样受仓库容量约束，满了就暂停不消耗原料）。
    /// 同一工坊的多条配方并行结算：原料够、产物没占满容量就各自产一次。
    /// 每座工坊有独立开关：整座停产（Tile.ProductionEnabled）或单独关掉某条产线（Tile.IsRecipeEnabled）。
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

                    TickWorkshop(pool, tile);
                }
            }
        }

        /// <summary>
        /// 结算一座工坊：逐条产线尝试生产。
        /// 抽成独立入口是为了让测试与 UI 能只针对某座工坊跑，而不必扫全图。
        /// </summary>
        public static void TickWorkshop(ResourcePool pool, Tile tile)
        {
            if (pool == null || tile == null)
            {
                return;
            }

            var recipes = CraftingCatalog.For(tile.Building);
            for (var i = 0; i < recipes.Count; i++)
            {
                TryProduce(pool, tile, recipes[i]);
            }
        }

        /// <summary>尝试结算一条产线；返回是否真的产出（判定不过就既不扣原料也不产出）。</summary>
        public static bool TryProduce(ResourcePool pool, Tile tile, RecipeDef recipe)
        {
            if (EvaluateRecipe(pool, tile, recipe) != RecipeState.Producing)
            {
                return false;
            }

            ConsumeInputs(pool, recipe);
            pool.Add(recipe.Output, recipe.OutputPerSecond);
            return true;
        }

        /// <summary>
        /// 判定一条产线此刻的状态。Tick 与 UI 面板都走这里，避免两边口径漂移。
        /// </summary>
        public static RecipeState EvaluateRecipe(ResourcePool pool, Tile tile, RecipeDef recipe)
        {
            if (pool == null || tile == null || recipe == null || recipe.OutputPerSecond <= 0)
            {
                return RecipeState.Disabled;
            }

            if (!tile.ProductionEnabled)
            {
                return RecipeState.WorkshopStopped;
            }

            if (!tile.IsRecipeEnabled(recipe.Id))
            {
                return RecipeState.Disabled;
            }

            if (!HasInputs(pool, recipe))
            {
                return RecipeState.MissingInputs;
            }

            return HasOutputSpace(pool, recipe) ? RecipeState.Producing : RecipeState.OutputFull;
        }

        private static bool HasInputs(ResourcePool pool, RecipeDef recipe)
        {
            var inputs = recipe.InputsPerSecond;
            for (var i = 0; i < inputs.Length; i++)
            {
                if (!pool.CanSpend(inputs[i].Type, inputs[i].AmountPerSecond))
                {
                    return false;
                }
            }

            return true;
        }

        private static void ConsumeInputs(ResourcePool pool, RecipeDef recipe)
        {
            var inputs = recipe.InputsPerSecond;
            for (var i = 0; i < inputs.Length; i++)
            {
                pool.TrySpend(inputs[i].Type, inputs[i].AmountPerSecond);
            }
        }

        /// <summary>容量差判断：产出不会因为容量截断而「白吃原料」。 </summary>
        private static bool HasOutputSpace(ResourcePool pool, RecipeDef recipe) =>
            pool.Capacity - pool.GetAmount(recipe.Output) >= recipe.OutputPerSecond;
    }
}
