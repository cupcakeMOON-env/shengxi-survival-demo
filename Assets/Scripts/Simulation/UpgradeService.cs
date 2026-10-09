namespace ShengXi.Simulation
{
    /// <summary>某座建筑此刻能不能升级，以及不能的原因（UI 与结算共用一套判定）。</summary>
    public enum UpgradeState
    {
        /// <summary>可以升级。</summary>
        Ok = 0,

        /// <summary>没有建筑，或者这类建筑不可升级（如采集站 / 仓库 / 工坊 / 据点）。</summary>
        NotUpgradable = 1,

        /// <summary>已经满级（MaxLevel）。</summary>
        MaxLevel = 2,

        /// <summary>建材不够。</summary>
        MissingMaterial = 3,

        /// <summary>行动点不够。</summary>
        MissingActionPoint = 4,
    }

    /// <summary>
    /// 升级服务：消耗「建材 + 行动点」把可升级建筑（当前为箭塔/围墙）升一级。
    /// 等级影响伤害与最大血量（BuildingStats 换算）；升级同时整修建筑至新等级满血，
    /// 因此白天用建材升级也兼具「修建筑」的作用。纯 C#，可单测。
    /// </summary>
    public static class UpgradeService
    {
        public const int MaxLevel = 3;
        public const int ActionPointCost = 1;

        public static bool CanUpgrade(GridMap map, ResourcePool pool, ActionPointSystem ap, GridPos pos) =>
            Evaluate(map, pool, ap, pos) == UpgradeState.Ok;

        /// <summary>
        /// 这座建筑有没有升级资格：只看"这类建筑能不能升级"，
        /// 不看建材与行动点（那是 Evaluate 的事）。UI 用它决定要不要弹升级面板。
        /// </summary>
        public static bool IsUpgradable(GridMap map, GridPos pos)
        {
            var tile = map?.GetTile(pos);
            var def = tile != null ? BuildingCatalog.Get(tile.Building) : null;
            return tile != null && def != null && def.UpgradeMaterialCost > 0;
        }

        /// <summary>
        /// 判定升级状态。UI 面板与 CanUpgrade 都走这里，
        /// 保证面板说的「建材不足」就是升级时真正被拦下的原因。
        /// </summary>
        public static UpgradeState Evaluate(GridMap map, ResourcePool pool, ActionPointSystem ap, GridPos pos)
        {
            if (map == null || pool == null || ap == null)
            {
                return UpgradeState.NotUpgradable;
            }

            if (!IsUpgradable(map, pos))
            {
                return UpgradeState.NotUpgradable;
            }

            var tile = map.GetTile(pos);
            var def = BuildingCatalog.Get(tile.Building);
            if (tile.BuildingLevel >= MaxLevel)
            {
                return UpgradeState.MaxLevel;
            }

            if (!pool.CanSpend(ResourceType.Material, def.UpgradeMaterialCost))
            {
                return UpgradeState.MissingMaterial;
            }

            return ap.CanSpend(ActionPointCost) ? UpgradeState.Ok : UpgradeState.MissingActionPoint;
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
