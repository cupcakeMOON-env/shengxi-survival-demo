namespace ShengXi.Simulation
{
    /// <summary>
    /// 工坊开关服务：把「这座工坊要不要开工」「哪几条产线要跑」这类玩家决策收敛到一处，
    /// 改完状态统一发事件，UI 与视图据此刷新。
    /// 纯 C#，不依赖 Unity，可单测。
    /// </summary>
    public static class WorkshopService
    {
        /// <summary>目标格是否是可控制的工坊。</summary>
        public static bool IsWorkshop(GridMap map, GridPos pos)
        {
            var tile = map?.GetTile(pos);
            return tile != null && tile.Building == BuildingType.Workshop;
        }

        /// <summary>
        /// 设置某座工坊开工 / 停产。
        /// 返回是否是一次有效调用（目标不是工坊返回 false）；状态本来就一致时返回 true 但不发事件。
        /// </summary>
        public static bool SetProducing(GridMap map, GridPos pos, bool producing)
        {
            var tile = map?.GetTile(pos);
            if (tile == null || tile.Building != BuildingType.Workshop)
            {
                return false;
            }

            if (tile.ProductionEnabled == producing)
            {
                return true;
            }

            tile.ProductionEnabled = producing;
            GameEvents.RaiseWorkshopStateChanged(pos);
            return true;
        }

        /// <summary>切换某座工坊的开工状态。</summary>
        public static bool ToggleProducing(GridMap map, GridPos pos)
        {
            var tile = map?.GetTile(pos);
            if (tile == null || tile.Building != BuildingType.Workshop)
            {
                return false;
            }

            return SetProducing(map, pos, !tile.ProductionEnabled);
        }

        /// <summary>
        /// 单独开关某条产线（按配方 Id）。
        /// 配方不属于该建筑、或目标不是工坊时返回 false。
        /// </summary>
        public static bool SetRecipeEnabled(GridMap map, GridPos pos, string recipeId, bool enabled)
        {
            var tile = map?.GetTile(pos);
            if (tile == null || tile.Building != BuildingType.Workshop)
            {
                return false;
            }

            if (CraftingCatalog.Find(tile.Building, recipeId) == null)
            {
                return false;
            }

            if (tile.IsRecipeEnabled(recipeId) == enabled)
            {
                return true;
            }

            tile.SetRecipeEnabled(recipeId, enabled);
            GameEvents.RaiseWorkshopStateChanged(pos);
            return true;
        }

        /// <summary>切换某条产线的开关。</summary>
        public static bool ToggleRecipe(GridMap map, GridPos pos, string recipeId)
        {
            var tile = map?.GetTile(pos);
            if (tile == null || tile.Building != BuildingType.Workshop)
            {
                return false;
            }

            return SetRecipeEnabled(map, pos, recipeId, !tile.IsRecipeEnabled(recipeId));
        }

        /// <summary>
        /// 是否处于「有效生产」：开工且至少有一条产线启用。
        /// 视图据此把停产（或所有产线都关了）的工坊画成暗色。
        /// </summary>
        public static bool IsEffectivelyProducing(Tile tile)
        {
            if (tile == null || tile.Building != BuildingType.Workshop || !tile.ProductionEnabled)
            {
                return false;
            }

            var recipes = CraftingCatalog.For(tile.Building);
            for (var i = 0; i < recipes.Count; i++)
            {
                if (tile.IsRecipeEnabled(recipes[i].Id))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 已开启产线的每秒收支，按 ResourceType 下标（负数=消耗，正数=产出）。
        /// 这是「配置产能」：只看开工与配方开关，不判断原料和容量是否满足——
        /// 那部分由 ProductionSystem.EvaluateRecipe 给出，面板会把两者分开显示。
        /// </summary>
        public static int[] ConfiguredRates(Tile tile)
        {
            var rates = new int[ResourcePool.ResourceTypeCount];
            if (tile == null || tile.Building != BuildingType.Workshop || !tile.ProductionEnabled)
            {
                return rates;
            }

            var recipes = CraftingCatalog.For(tile.Building);
            for (var i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                if (!tile.IsRecipeEnabled(recipe.Id))
                {
                    continue;
                }

                var inputs = recipe.InputsPerSecond;
                for (var j = 0; j < inputs.Length; j++)
                {
                    rates[(int)inputs[j].Type] -= inputs[j].AmountPerSecond;
                }

                rates[(int)recipe.Output] += recipe.OutputPerSecond;
            }

            return rates;
        }
    }
}
