using System.Collections.Generic;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 建筑目录：所有建筑定义的唯一数据源（数据驱动）。
    /// </summary>
    public static class BuildingCatalog
    {
        public static readonly IReadOnlyList<BuildingDef> All = new List<BuildingDef>
        {
            new BuildingDef
            {
                Type = BuildingType.Collector,
                Name = "采集站",
                WoodCost = 5,
                StoneCost = 3,
                ActionPointCost = 1,
                MaxHp = 5,
                RequiresResourceTile = true,
                Description = "建在资源格上，每秒自动采集 1 个资源",
            },
            new BuildingDef
            {
                Type = BuildingType.Warehouse,
                Name = "仓库",
                WoodCost = 10,
                StoneCost = 5,
                ActionPointCost = 2,
                CapacityBonus = 100,
                MaxHp = 5,
                Description = "资源容量上限 +100",
            },
            new BuildingDef
            {
                Type = BuildingType.Wall,
                Name = "围墙",
                WoodCost = 2,
                ActionPointCost = 1,
                MaxHp = 3,
                Description = "阻挡敌人通行；敌人会优先攻击它（M3 生效）",
            },
            new BuildingDef
            {
                Type = BuildingType.ArrowTower,
                Name = "箭塔",
                WoodCost = 15,
                StoneCost = 10,
                ActionPointCost = 3,
                MaxHp = 5,
                Range = 3,
                Damage = 1,
                Description = "自动攻击敌人（M3 生效）",
            },
            new BuildingDef
            {
                Type = BuildingType.Workshop,
                Name = "工坊",
                WoodCost = 8,
                StoneCost = 6,
                ActionPointCost = 2,
                MaxHp = 5,
                Description = "加工建材（后续开放）",
            },
        };

        private static readonly Dictionary<BuildingType, BuildingDef> ByType = BuildLookup();

        public static BuildingDef Get(BuildingType type) =>
            ByType.TryGetValue(type, out var def) ? def : null;

        private static Dictionary<BuildingType, BuildingDef> BuildLookup()
        {
            var dict = new Dictionary<BuildingType, BuildingDef>();
            foreach (var def in All)
            {
                dict[def.Type] = def;
            }

            return dict;
        }
    }
}
