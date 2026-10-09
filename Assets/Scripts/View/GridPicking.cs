using ShengXi.Simulation;
using UnityEngine;

namespace ShengXi.View
{
    /// <summary>
    /// 屏幕 / 世界坐标 → 格子坐标的唯一换算入口。
    ///
    /// 这里必须用 RoundToInt，不能用 FloorToInt：
    /// GridView 把第 (x, y) 格画在 world = (x, y)、边长 0.96（也就是格子以整数坐标为中心），
    /// 而 FloorToInt 的取整边界落在整数上，于是每格「可点区域」整体比「可见格子」左移半格——
    /// 表现就是点一个格子的左半边会选中左边那一格，血条和按钮都对得上、但点不中想看的东西。
    /// </summary>
    public static class GridPicking
    {
        /// <summary>世界坐标 → 格子坐标（格心在整数坐标上）。</summary>
        public static GridPos WorldToGrid(Vector3 world) =>
            new GridPos(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));

        /// <summary>屏幕坐标 → 格子坐标（相机为空时返回 null）。</summary>
        public static GridPos? ScreenToGrid(Camera camera, Vector3 screenPosition)
        {
            if (camera == null)
            {
                return null;
            }

            return WorldToGrid(camera.ScreenToWorldPoint(screenPosition));
        }
    }
}
