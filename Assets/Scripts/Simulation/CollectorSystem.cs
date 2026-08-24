namespace ShengXi.Simulation
{
    /// <summary>
    /// 采集站自动采集：每个 tick 扫描地图，让每个采集站从自己所在的资源格采集。
    /// 复用 CollectService 的采集逻辑，保证与手动点击行为一致。
    /// </summary>
    public static class CollectorSystem
    {
        public static void Tick(GridMap map, ResourcePool pool)
        {
            if (map == null || pool == null)
            {
                return;
            }

            for (var x = 0; x < map.Width; x++)
            {
                for (var y = 0; y < map.Height; y++)
                {
                    var pos = new GridPos(x, y);
                    var tile = map.GetTile(pos);
                    if (tile != null && tile.Building == BuildingType.Collector)
                    {
                        CollectService.TryCollect(map, pool, pos);
                    }
                }
            }
        }
    }
}
