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
    }
}
