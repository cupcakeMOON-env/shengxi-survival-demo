namespace ShengXi.Simulation
{
    /// <summary>
    /// 修理服务：白天消耗 1 修理包 + 1 行动点，把残血建筑或据点直接修满。
    /// 修理包由工坊按配方产出（CraftingCatalog），是「木/石/食物 → 防御续航」的消耗口。
    /// 据点不在地图建筑目录里，单独提供 CanRepairBase / TryRepairBase。纯 C#，可单测。
    /// </summary>
    public static class RepairService
    {
        public const int ActionPointCost = 1;
        public const int KitCost = 1;

        public static bool CanRepairBuilding(GridMap map, ResourcePool pool, ActionPointSystem ap, GridPos pos)
        {
            if (map == null || pool == null || ap == null)
            {
                return false;
            }

            var tile = map.GetTile(pos);
            var def = tile != null ? BuildingCatalog.Get(tile.Building) : null;
            if (tile == null || def == null)
            {
                return false; // 空地或据点（据点走 CanRepairBase）
            }

            var maxHp = BuildingStats.MaxHp(def, tile.BuildingLevel);
            return tile.BuildingHp < maxHp && HasSupplies(pool, ap);
        }

        public static bool TryRepairBuilding(GridMap map, ResourcePool pool, ActionPointSystem ap, GridPos pos)
        {
            if (!CanRepairBuilding(map, pool, ap, pos))
            {
                return false;
            }

            var tile = map.GetTile(pos);
            var def = BuildingCatalog.Get(tile.Building);
            pool.TrySpend(ResourceType.RepairKit, KitCost);
            ap.Spend(ActionPointCost);

            var maxHp = BuildingStats.MaxHp(def, tile.BuildingLevel);
            tile.BuildingHp = maxHp;
            GameEvents.RaiseBuildingRepaired(pos, tile.BuildingHp, maxHp);
            return true;
        }

        public static bool CanRepairBase(Base baseDefense, ResourcePool pool, ActionPointSystem ap) =>
            baseDefense != null &&
            !baseDefense.IsDestroyed &&
            baseDefense.CurrentHp < baseDefense.MaxHp &&
            HasSupplies(pool, ap);

        public static bool TryRepairBase(Base baseDefense, ResourcePool pool, ActionPointSystem ap)
        {
            if (!CanRepairBase(baseDefense, pool, ap))
            {
                return false;
            }

            pool.TrySpend(ResourceType.RepairKit, KitCost);
            ap.Spend(ActionPointCost);
            baseDefense.RepairToFull();
            return true;
        }

        private static bool HasSupplies(ResourcePool pool, ActionPointSystem ap) =>
            pool != null && ap != null &&
            pool.CanSpend(ResourceType.RepairKit, KitCost) &&
            ap.CanSpend(ActionPointCost);
    }
}
