namespace ShengXi.Simulation
{
    /// <summary>
    /// 建造服务：校验 → 扣资源/行动点 → 落格 → 应用效果 → 发事件。
    /// 纯 C#，可单测。
    /// </summary>
    public static class BuildService
    {
        public static bool CanBuild(GridMap map, ResourcePool pool, ActionPointSystem ap, BuildingDef def, GridPos pos)
        {
            if (def == null || map == null || pool == null || ap == null)
            {
                return false;
            }

            if (!map.CanPlace(def.Type, pos))
            {
                return false;
            }

            var tile = map.GetTile(pos);
            if (def.RequiresResourceTile &&
                (tile == null || tile.ResourceAmount <= 0 || CollectService.ResourceTypeFor(tile.Terrain) == null))
            {
                return false;
            }

            if (!pool.CanSpend(ResourceType.Wood, def.WoodCost) ||
                !pool.CanSpend(ResourceType.Stone, def.StoneCost) ||
                !pool.CanSpend(ResourceType.Food, def.FoodCost))
            {
                return false;
            }

            return ap.CanSpend(def.ActionPointCost);
        }

        public static bool TryBuild(GridMap map, ResourcePool pool, ActionPointSystem ap, BuildingDef def, GridPos pos)
        {
            if (!CanBuild(map, pool, ap, def, pos))
            {
                return false;
            }

            pool.TrySpend(ResourceType.Wood, def.WoodCost);
            pool.TrySpend(ResourceType.Stone, def.StoneCost);
            pool.TrySpend(ResourceType.Food, def.FoodCost);
            ap.Spend(def.ActionPointCost);

            map.Place(def.Type, pos);

            if (def.CapacityBonus > 0)
            {
                pool.Capacity += def.CapacityBonus;
            }

            GameEvents.RaiseBuildingPlaced(pos, def.Type);
            return true;
        }
    }
}
