using System.Collections.Generic;

namespace ShengXi.Simulation
{
    /// <summary>某类建筑的建成数量（结算页明细用）。</summary>
    public class BuildingCount
    {
        public BuildingType Type;
        public int Count;
    }

    /// <summary>
    /// 一局结算的数据快照：纯数据，UI 直接读它渲染结算页，不回头翻模拟对象。
    /// 只在结束时构建一次，之后局面冻结，所以不需要考虑它会过期。
    /// </summary>
    public class RunSummary
    {
        public RunOutcome Outcome;
        public int Day;
        public bool IsNight;
        public int ActionPoints;
        public int BaseHp;
        public int BaseMaxHp;
        public int Capacity;
        public int Wood;
        public int Stone;
        public int Food;
        public int Material;
        public int RepairKit;
        public int TotalBuildings;
        public readonly List<BuildingCount> Buildings = new List<BuildingCount>();
    }

    /// <summary>
    /// 结算服务：把当前局面拍成一份 <see cref="RunSummary"/>。
    /// 纯 C#，可脱离 Unity 单测；不做任何写操作，只读。
    /// </summary>
    public static class SettlementService
    {
        public static RunSummary Build(
            GridMap map,
            ResourcePool pool,
            ActionPointSystem actionPoints,
            DayCycle cycle,
            Base baseDefense,
            RunOutcome outcome)
        {
            var summary = new RunSummary { Outcome = outcome };

            if (cycle != null)
            {
                summary.Day = cycle.Day;
                summary.IsNight = cycle.IsNight;
            }

            if (actionPoints != null)
            {
                summary.ActionPoints = actionPoints.Current;
            }

            if (baseDefense != null)
            {
                summary.BaseHp = baseDefense.CurrentHp;
                summary.BaseMaxHp = baseDefense.MaxHp;
            }

            if (pool != null)
            {
                summary.Capacity = pool.Capacity;
                summary.Wood = pool.GetAmount(ResourceType.Wood);
                summary.Stone = pool.GetAmount(ResourceType.Stone);
                summary.Food = pool.GetAmount(ResourceType.Food);
                summary.Material = pool.GetAmount(ResourceType.Material);
                summary.RepairKit = pool.GetAmount(ResourceType.RepairKit);
            }

            CollectBuildings(map, summary);
            return summary;
        }

        /// <summary>
        /// 建筑明细文字，例如「采集站 2 · 工坊 1 · 箭塔 3」。
        /// 按 BuildingType 的枚举顺序输出，保证同一局面每次渲染顺序一致（截图/测试不会抖）。
        /// 据点不计入建筑数——它是每局都有的，写进去只会让数字看起来虚高。
        /// </summary>
        public static string DescribeBuildings(RunSummary summary)
        {
            if (summary == null || summary.Buildings.Count == 0)
            {
                return "无";
            }

            var parts = new List<string>();
            foreach (var entry in summary.Buildings)
            {
                var def = BuildingCatalog.Get(entry.Type);
                var name = def != null ? def.Name : entry.Type.ToString();
                parts.Add($"{name} {entry.Count}");
            }

            return string.Join(" · ", parts);
        }

        private static void CollectBuildings(GridMap map, RunSummary summary)
        {
            if (map == null)
            {
                return;
            }

            foreach (var def in BuildingCatalog.All)
            {
                if (def.Type == BuildingType.Base)
                {
                    continue;
                }

                var count = 0;
                foreach (var pos in map.FindBuildingPositions(def.Type))
                {
                    var tile = map.GetTile(pos);
                    // 被打到 0 血、正等着清理的建筑不算数
                    if (tile != null && tile.BuildingHp > 0)
                    {
                        count++;
                    }
                }

                if (count == 0)
                {
                    continue;
                }

                summary.Buildings.Add(new BuildingCount { Type = def.Type, Count = count });
                summary.TotalBuildings += count;
            }
        }
    }
}
