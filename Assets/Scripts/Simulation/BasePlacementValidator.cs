namespace ShengXi.Simulation
{
    /// <summary>
    /// 据点放置校验：必须是地图内、可通行、未被占用的格子。
    /// 纯 C# 可单测。
    /// </summary>
    public static class BasePlacementValidator
    {
        public static bool CanPlace(GridMap map, GridPos pos)
        {
            if (map == null)
            {
                return false;
            }

            var tile = map.GetTile(pos);
            return tile != null && tile.IsWalkable && tile.Building == BuildingType.None;
        }
    }
}
