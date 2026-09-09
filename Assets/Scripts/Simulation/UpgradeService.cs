namespace ShengXi.Simulation
{
    /// <summary>
    /// 升级服务：消耗「建材 + 行动点」把可升级建筑（当前为箭塔/围墙）升一级。
    /// 等级影响伤害与最大血量（BuildingStats 换算）；升级同时整修建筑至新等级满血，
    /// 因此白天用建材升级也兼具「修建筑」的作用。纯 C#，可单测。
    /// </summary>
    public static class UpgradeService
    {
        public const int MaxLevel = 3;
        public const int ActionPointCost = 1;

        public static bool CanUpgrade(GridMap map, ResourcePool pool, ActionPointSystem ap, GridPos pos)
        {
            if (map == null || pool == null || ap == null)
            {
                return false;
            }

            var tile = map.GetTile(pos);
            var def = tile != null ? BuildingCatalog.Get(tile.Building) : null;
            if (tile == null || def == null || def.UpgradeMaterialCost <= 0)
            {
                return false;
            }

            if (tile.BuildingLevel >= MaxLevel)
            {
                return false;
            }

            return pool.CanSpend(ResourceType.Material, def.UpgradeMaterialCost) &&
                   ap.CanSpend(ActionPointCost);
        }

        public static bool TryUpgrade(GridMap map, ResourcePool pool, ActionPointSystem ap, GridPos pos)
        {
            if (!CanUpgrade(map, pool, ap, pos))
            {
                return false;
            }

            var tile = map.GetTile(pos);
            var def = BuildingCatalog.Get(tile.Building);
            pool.TrySpend(ResourceType.Material, def.UpgradeMaterialCost);
            ap.Spend(ActionPointCost);

            tile.BuildingLevel++;
            tile.BuildingHp = BuildingStats.MaxHp(def, tile.BuildingLevel);
            GameEvents.RaiseBuildingUpgraded(pos, tile.Building, tile.BuildingLevel);
            return true;
        }
    }
}
