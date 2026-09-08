using System.Collections.Generic;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 防御建筑食物供给（当前只有箭塔会消耗）：
    /// 夜晚战斗期间每 1 秒结算一次。所有带 FoodPerSecond 消耗的存活建筑按「总需求」统一判定——
    /// 食物足以覆盖全部需求时扣除并保持供给；不足时全部断粮停火、不扣费；
    /// 食物恢复后下一个结算自动复工（状态放在 Tile.BuildingStarved，瞬态、不存档）。
    /// </summary>
    public static class FoodSupplySystem
    {
        /// <summary>供给结算间隔（秒），与采集站 tick 一致，方便“每座塔每秒消耗 N 食物”的口径。</summary>
        public const float TickInterval = 1f;

        /// <summary>
        /// 夜晚开始时按当前食物储备刷新状态（不扣费）：
        /// 避免开局头一秒内箭塔在没粮时仍然“免费开火”。
        /// </summary>
        public static void Refresh(GridMap map, ResourcePool pool)
        {
            Evaluate(map, pool, charge: false);
        }

        /// <summary>每个供给 tick（1 秒）：判定 + 扣费 + 发状态变更事件。</summary>
        public static void Tick(GridMap map, ResourcePool pool)
        {
            Evaluate(map, pool, charge: true);
        }

        /// <summary>夜晚结束（回白天）：全部防御建筑恢复供给，白天不再消耗食物。</summary>
        public static void RestoreAll(GridMap map)
        {
            if (map == null)
            {
                return;
            }

            for (var x = 0; x < map.Width; x++)
            {
                for (var y = 0; y < map.Height; y++)
                {
                    var pos = new GridPos(x, y);
                    var tile = map.GetTile(pos);
                    var def = tile != null ? BuildingCatalog.Get(tile.Building) : null;
                    if (def == null || def.FoodPerSecond <= 0 || tile.BuildingHp <= 0 || !tile.BuildingStarved)
                    {
                        continue;
                    }

                    tile.BuildingStarved = false;
                    GameEvents.RaiseBuildingSupplyChanged(pos, true);
                }
            }
        }

        /// <summary>箭塔开火前的供给查询：无消耗的建筑恒为可工作。</summary>
        public static bool IsSupplied(GridMap map, GridPos pos)
        {
            var tile = map?.GetTile(pos);
            if (tile == null)
            {
                return false;
            }

            var def = BuildingCatalog.Get(tile.Building);
            if (def == null || def.FoodPerSecond <= 0)
            {
                return true;
            }

            return !tile.BuildingStarved;
        }

        private static void Evaluate(GridMap map, ResourcePool pool, bool charge)
        {
            if (map == null || pool == null)
            {
                return;
            }

            var positions = new List<GridPos>();
            var demand = 0;
            for (var x = 0; x < map.Width; x++)
            {
                for (var y = 0; y < map.Height; y++)
                {
                    var pos = new GridPos(x, y);
                    var tile = map.GetTile(pos);
                    var def = tile != null ? BuildingCatalog.Get(tile.Building) : null;
                    if (def == null || def.FoodPerSecond <= 0 || tile.BuildingHp <= 0)
                    {
                        continue;
                    }

                    positions.Add(pos);
                    demand += def.FoodPerSecond;
                }
            }

            if (positions.Count == 0)
            {
                return;
            }

            var supplied = pool.CanSpend(ResourceType.Food, demand);
            if (supplied && charge)
            {
                pool.TrySpend(ResourceType.Food, demand);
            }

            foreach (var pos in positions)
            {
                var tile = map.GetTile(pos);
                var targetStarved = !supplied;
                if (tile.BuildingStarved == targetStarved)
                {
                    continue;
                }

                tile.BuildingStarved = targetStarved;
                GameEvents.RaiseBuildingSupplyChanged(pos, supplied);
            }
        }
    }
}
