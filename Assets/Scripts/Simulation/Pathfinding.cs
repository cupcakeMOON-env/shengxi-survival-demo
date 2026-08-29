using System.Collections.Generic;

namespace ShengXi.Simulation
{
    /// <summary>
    /// BFS 最短路径（4 方向），只走可通行格子（绕开水与围墙）。
    /// 返回 from 到 target 的下一步；无路可走返回 null。
    /// </summary>
    public static class Pathfinding
    {
        private static readonly GridPos[] Directions =
        {
            new GridPos(0, 1),
            new GridPos(0, -1),
            new GridPos(-1, 0),
            new GridPos(1, 0),
        };

        public static GridPos? NextStep(GridMap map, GridPos from, GridPos target)
        {
            if (from == target)
            {
                return target;
            }

            var prev = new Dictionary<GridPos, GridPos> { [from] = from };
            var queue = new Queue<GridPos>();
            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target)
                {
                    break;
                }

                foreach (var dir in Directions)
                {
                    var next = new GridPos(current.X + dir.X, current.Y + dir.Y);
                    if (!map.IsInside(next) || prev.ContainsKey(next))
                    {
                        continue;
                    }

                    var tile = map.GetTile(next);
                    if (tile == null || !tile.IsWalkable)
                    {
                        continue;
                    }

                    prev[next] = current;
                    queue.Enqueue(next);
                }
            }

            if (!prev.ContainsKey(target))
            {
                return null;
            }

            var step = target;
            while (step != from && prev[step] != from)
            {
                step = prev[step];
            }

            return step;
        }

        /// <summary>
        /// BFS 连通性检查：from 是否能沿可通行格子走到 target（target 本身可通行即可达，
        /// 与敌人寻路到据点贴邻攻击的语义一致）。
        /// </summary>
        public static bool IsReachable(GridMap map, GridPos from, GridPos target)
        {
            if (map == null || !map.IsInside(from) || !map.IsInside(target))
            {
                return false;
            }

            if (from == target)
            {
                return true;
            }

            var visited = new HashSet<GridPos> { from };
            var queue = new Queue<GridPos>();
            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target)
                {
                    return true;
                }

                foreach (var dir in Directions)
                {
                    var next = new GridPos(current.X + dir.X, current.Y + dir.Y);
                    if (!map.IsInside(next) || visited.Contains(next))
                    {
                        continue;
                    }

                    var tile = map.GetTile(next);
                    if (tile == null || !tile.IsWalkable)
                    {
                        continue;
                    }

                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        /// <summary>
        /// BFS 连通性检查：from 是否能沿可通行格子走到任意地图边缘格。
        /// 用于据点放置校验——据点必须能连通地图边缘，否则敌人无法从边缘刷怪点到达。
        /// </summary>
        public static bool IsReachableFromAnyEdge(GridMap map, GridPos from)
        {
            if (map == null || !map.IsInside(from))
            {
                return false;
            }

            var visited = new HashSet<GridPos> { from };
            var queue = new Queue<GridPos>();
            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.X == 0 || current.X == map.Width - 1 || current.Y == 0 || current.Y == map.Height - 1)
                {
                    return true;
                }

                foreach (var dir in Directions)
                {
                    var next = new GridPos(current.X + dir.X, current.Y + dir.Y);
                    if (!map.IsInside(next) || visited.Contains(next))
                    {
                        continue;
                    }

                    var tile = map.GetTile(next);
                    if (tile == null || !tile.IsWalkable)
                    {
                        continue;
                    }

                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        /// <summary>
        /// 朝不可通行的建筑走一步：目标是建筑四周任意可达的可行走格子（最近者优先）。
        /// 敌人贴近建筑后由 CombatSim 执行攻击，不再继续移动。
        /// </summary>
        public static GridPos? NextStepToBuilding(GridMap map, GridPos from, GridPos buildingPos)
        {
            if (map == null || !map.IsInside(buildingPos))
            {
                return null;
            }

            var prev = new Dictionary<GridPos, GridPos> { [from] = from };
            var queue = new Queue<GridPos>();
            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (IsAdjacent(current, buildingPos))
                {
                    var step = current;
                    while (step != from && prev[step] != from)
                    {
                        step = prev[step];
                    }

                    return step;
                }

                foreach (var dir in Directions)
                {
                    var next = new GridPos(current.X + dir.X, current.Y + dir.Y);
                    if (!map.IsInside(next) || prev.ContainsKey(next))
                    {
                        continue;
                    }

                    var tile = map.GetTile(next);
                    if (tile == null || !tile.IsWalkable)
                    {
                        continue;
                    }

                    prev[next] = current;
                    queue.Enqueue(next);
                }
            }

            return null;
        }

        /// <summary>四方向相邻（曼哈顿距离 1），与 CombatSim 的攻击判定一致。</summary>
        public static bool IsAdjacent(GridPos a, GridPos b)
        {
            return System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y) == 1;
        }
    }
}
