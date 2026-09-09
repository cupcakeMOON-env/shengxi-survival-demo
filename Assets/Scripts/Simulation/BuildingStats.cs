namespace ShengXi.Simulation
{
    /// <summary>
    /// 建筑属性按等级换算的唯一入口（伤害 / 最大血量）。
    /// 箭塔升级加伤害、围墙/箭塔升级加血量都从这里取数，
    /// 避免 CombatSim / GridView / SaveMigrator 各自手写等级公式。
    /// </summary>
    public static class BuildingStats
    {
        public static int Damage(BuildingDef def, int level)
        {
            if (def == null)
            {
                return 0;
            }

            var lv = Normalize(level);
            return def.Damage + (lv - 1) * def.DamagePerLevel;
        }

        public static int MaxHp(BuildingDef def, int level)
        {
            if (def == null)
            {
                return 0;
            }

            var lv = Normalize(level);
            return def.MaxHp + (lv - 1) * def.HpPerLevel;
        }

        /// <summary>读取格子上的建筑在当前等级下的属性；无有效建筑返回 0。</summary>
        public static int DamageAt(GridMap map, GridPos pos)
        {
            var tile = map?.GetTile(pos);
            if (tile == null)
            {
                return 0;
            }

            return Damage(BuildingCatalog.Get(tile.Building), tile.BuildingLevel);
        }

        public static int MaxHpAt(GridMap map, GridPos pos)
        {
            var tile = map?.GetTile(pos);
            if (tile == null)
            {
                return 0;
            }

            return MaxHp(BuildingCatalog.Get(tile.Building), tile.BuildingLevel);
        }

        private static int Normalize(int level) => level < 1 ? 1 : level;
    }
}
