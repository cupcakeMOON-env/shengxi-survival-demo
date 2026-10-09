using ShengXi.Core;
using ShengXi.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ShengXi.View
{
    /// <summary>
    /// 点击地图格子触发采集。
    /// 坐标换算链路：屏幕坐标 → 世界坐标 → GridPos → CollectService。
    /// </summary>
    public class TileClickInput : MonoBehaviour
    {
        private Camera _camera;
        private BuildModeController _buildMode;

        /// <summary>
        /// 由 GameBootstrap 直接注入相机与建造模式引用。
        /// </summary>
        public void Initialize(Camera camera, BuildModeController buildMode)
        {
            _camera = camera;
            _buildMode = buildMode;
        }

        private void Start()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    Debug.LogWarning("[TileClickInput] 没有找到 Main Camera");
                }
            }
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (GameLoop.Instance == null)
            {
                Debug.LogWarning("[TileClickInput] GameLoop 未初始化");
                return;
            }

            // 开始界面阶段没有世界，别碰模拟数据
            if (!GameLoop.Instance.HasWorld)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (GameLoop.Instance.GameOver)
            {
                return;
            }

            if (GameLoop.Instance.IsChoosingBase)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                Debug.LogWarning("[TileClickInput] 没有找到 Main Camera，无法换算坐标");
                return;
            }

            var pos = GridPicking.ScreenToGrid(_camera, Input.mousePosition);
            if (pos.HasValue)
            {
                ResolveClick(pos.Value);
            }
        }

        /// <summary>
        /// 处理一次格子点击：处于建造 / 拆除 / 升级 / 修理模式且是白天时，先执行模式动作；
        /// 这次点击没有执行成任何操作的话——点的是工坊就打开它的面板，点其他格子则在没有模式时收起面板。
        /// 抽成独立方法的目的是让 PlayMode 测试能走同一条路径：测试里没法注入真实的鼠标事件。
        /// </summary>
        public void ResolveClick(GridPos pos)
        {
            var loop = GameLoop.Instance;
            if (loop == null)
            {
                return;
            }

            var hasMode = _buildMode != null && _buildMode.HasActiveMode;

            // 建造 / 拆除 / 升级 / 修理只在白天生效；
            // 查看与开关工坊不消耗行动点，白天夜晚都能用
            var canRunMode = hasMode && loop.Cycle.IsDay;

            var handled = false;
            if (canRunMode && _buildMode.ActiveDef != null)
            {
                handled = BuildService.TryBuild(
                    loop.Map,
                    loop.Pool,
                    loop.ActionPoints,
                    _buildMode.ActiveDef,
                    pos);
                Debug.Log($"[TileClickInput] 建造 {_buildMode.ActiveDef.Name} grid={pos} ok={handled}");
            }
            else if (canRunMode && _buildMode.IsDemolishing)
            {
                handled = DemolishService.TryDemolish(loop.Map, loop.Pool, pos);
                Debug.Log($"[TileClickInput] 拆除 grid={pos} ok={handled}");
            }
            else if (canRunMode && _buildMode.IsRepairing)
            {
                handled = loop.Base != null && loop.Base.Position == pos
                    ? RepairService.TryRepairBase(loop.Base, loop.Pool, loop.ActionPoints)
                    : RepairService.TryRepairBuilding(loop.Map, loop.Pool, loop.ActionPoints, pos);
                Debug.Log($"[TileClickInput] 修理 grid={pos} ok={handled}");
            }

            if (handled)
            {
                return;
            }

            // 兜底：这次点击什么操作都没执行成时，按建筑类型打开对应面板——
            // 工坊开产线面板，箭塔 / 围墙开升级面板（升级不再是"先点模式再点目标"，
            // 改成点建筑出面板、在面板上确认）。建造模式下点已占格也走这里，
            // 打开面板比悄无反应有用。其他格子只在没有操作模式时才收起面板，避免放建筑时误关。
            if (WorkshopService.IsWorkshop(loop.Map, pos))
            {
                Debug.Log($"[TileClickInput] 打开工坊面板 grid={pos}");
                GameEvents.RaiseBuildingSelected(pos);
            }
            else if (UpgradeService.IsUpgradable(loop.Map, pos))
            {
                Debug.Log($"[TileClickInput] 打开升级面板 grid={pos}");
                GameEvents.RaiseBuildingSelected(pos);
            }
            else if (!hasMode)
            {
                Debug.Log($"[TileClickInput] 收起建筑面板 grid={pos}");
                GameEvents.RaiseBuildingSelected(null);
            }
        }
    }
}
