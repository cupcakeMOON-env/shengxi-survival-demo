namespace ShengXi.Simulation
{
    /// <summary>
    /// 拆除服务：校验 → 扣行动点 → 返还一半造价 → 撤销建筑效果（如仓库容量）→ 清除格子 → 发事件。
    /// 纯 C#，可单测；GridMap.RemoveBuilding 是底层操作，这里补全资源 / 行动点 / 效果 / 事件闭环。
    /// 拆除只能在白天进行（由上层 BuildModeController / TileClickInput 保证），据点不可拆除。
    /// </summary>
    public static class DemolishService
    {
        public const int ActionPointCost = 1;
        public const int RefundPercent = 50;

        public static bool CanDemolish(GridMap map, ActionPointSystem ap, GridPos pos)
        {
            if (map == null || ap == null)
            {
                return false;
            }

            var tile = map.GetTile(pos);
            if (tile == null || tile.Building == BuildingType.None || tile.Building == BuildingType.Base)
            {
                return false;
            }

            return ap.CanSpend(ActionPointCost);
        }

        public static bool TryDemolish(GridMap map, ResourcePool pool, ActionPointSystem ap, GridPos pos)
        {
            if (!CanDemolish(map, ap, pos))
            {
                return false;
            }

            var def = BuildingCatalog.Get(map.GetTile(pos).Building);

            ap.Spend(ActionPointCost);

            if (def != null)
            {
                Refund(pool, ResourceType.Wood, def.WoodCost);
                Refund(pool, ResourceType.Stone, def.StoneCost);
                Refund(pool, ResourceType.Food, def.FoodCost);
            }

            return DestroyBuilding(map, pool, pos);
        }

        /// <summary>
        /// 直接摧毁建筑（敌人攻击用）：撤销效果（仓库容量回落）、清除格子、发事件；
        /// 不返还资源、不扣行动点。据点不可被此方法摧毁。
        /// </summary>
        public static bool DestroyBuilding(GridMap map, ResourcePool pool, GridPos pos)
        {
            var tile = map?.GetTile(pos);
            if (tile == null || tile.Building == BuildingType.None || tile.Building == BuildingType.Base)
            {
                return false;
            }

            var def = BuildingCatalog.Get(tile.Building);
            if (def != null && def.CapacityBonus > 0 && pool != null)
            {
                pool.ReduceCapacity(def.CapacityBonus);
            }

            map.RemoveBuilding(pos);
            GameEvents.RaiseBuildingRemoved(pos);
            return true;
        }

        private static void Refund(ResourcePool pool, ResourceType type, int cost)
        {
            var refund = cost * RefundPercent / 100;
            if (refund > 0)
            {
                pool.Add(type, refund);
            }
        }
    }
}
