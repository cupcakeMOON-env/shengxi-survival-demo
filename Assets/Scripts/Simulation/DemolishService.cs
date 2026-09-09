namespace ShengXi.Simulation
{
    /// <summary>
    /// 拆除服务：校验 → 返还一半造价 → 撤销建筑效果（如仓库容量）→ 清除格子 → 发事件。
    /// 纯 C#，可单测；GridMap.RemoveBuilding 是底层操作，这里补全资源 / 行动点 / 效果 / 事件闭环。
    /// 拆除不消耗行动点（2026-08-28 调整）；只能在白天进行（由上层 BuildModeController / TileClickInput 保证），据点不可拆除。
    /// </summary>
    public static class DemolishService
    {
        public const int RefundPercent = 50;

        public static bool CanDemolish(GridMap map, GridPos pos)
        {
            if (map == null)
            {
                return false;
            }

            var tile = map.GetTile(pos);
            if (tile == null || tile.Building == BuildingType.None || tile.Building == BuildingType.Base)
            {
                return false;
            }

            return true;
        }

        public static bool TryDemolish(GridMap map, ResourcePool pool, GridPos pos)
        {
            if (pool == null || !CanDemolish(map, pos))
            {
                return false;
            }

            var tile = map.GetTile(pos);
            var def = BuildingCatalog.Get(tile.Building);

            if (def != null)
            {
                Refund(pool, ResourceType.Wood, def.WoodCost);
                Refund(pool, ResourceType.Stone, def.StoneCost);
                Refund(pool, ResourceType.Food, def.FoodCost);
                // 升级投入的建材同样返还一半（等级 1 时自然为 0）
                var invested = def.UpgradeMaterialCost * (tile.BuildingLevel - 1);
                Refund(pool, ResourceType.Material, invested);
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
