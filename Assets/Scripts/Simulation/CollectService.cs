namespace ShengXi.Simulation
{
    /// <summary>
    /// 采集服务：资源格 → 扣格子余量 → 加进资源池 → 发事件。
    /// 纯 C# 可单测。手动点击采集已移除，现在由采集站（CollectorSystem）每 tick 调用。
    /// 资源采空后格子变回草地（视觉反馈，也防止继续采）。
    /// </summary>
    public static class CollectService
    {
        public const int YieldPerCollect = 1;

        public static bool TryCollect(GridMap map, ResourcePool pool, GridPos pos)
        {
            var tile = map.GetTile(pos);
            if (tile == null || tile.ResourceAmount <= 0)
            {
                return false;
            }

            var resource = ResourceTypeFor(tile.Terrain);
            if (resource == null)
            {
                return false;
            }

            tile.ResourceAmount -= YieldPerCollect;
            pool.Add(resource.Value, YieldPerCollect);

            if (tile.ResourceAmount <= 0)
            {
                map.SetTerrain(pos, TerrainType.Grass, 0);
                GameEvents.RaiseTerrainChanged(pos, TerrainType.Grass);
            }

            return true;
        }

        public static ResourceType? ResourceTypeFor(TerrainType terrain) => terrain switch
        {
            TerrainType.Forest => ResourceType.Wood,
            TerrainType.Stone => ResourceType.Stone,
            TerrainType.Bush => ResourceType.Food,
            _ => null,
        };
    }
}
