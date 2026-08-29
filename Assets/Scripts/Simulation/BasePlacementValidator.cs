namespace ShengXi.Simulation
{
    /// <summary>
    /// 据点放置校验：必须是地图内、可通行、未被占用的格子，且能连通地图边缘。
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
            if (tile == null || !tile.IsWalkable || tile.Building != BuildingType.None)
            {
                return false;
            }

            // 据点必须能连通地图边缘：否则敌人无法从边缘刷怪点走到据点，
            // 夜晚会变成「敌人卡死在出生点」的死局（历史 PlayMode 超时根因）。
            return Pathfinding.IsReachableFromAnyEdge(map, pos);
        }
    }
}
