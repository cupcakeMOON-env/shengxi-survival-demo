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

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (GameLoop.Instance.GameOver || !GameLoop.Instance.Cycle.IsDay)
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

            var world = _camera.ScreenToWorldPoint(Input.mousePosition);
            var pos = new GridPos(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));

            if (_buildMode != null && _buildMode.ActiveDef != null)
            {
                var built = BuildService.TryBuild(
                    GameLoop.Instance.Map,
                    GameLoop.Instance.Pool,
                    GameLoop.Instance.ActionPoints,
                    _buildMode.ActiveDef,
                    pos);
                Debug.Log($"[TileClickInput] 建造 {_buildMode.ActiveDef.Name} grid={pos} ok={built}");
            }
            else
            {
                var collected = CollectService.TryCollect(GameLoop.Instance.Map, GameLoop.Instance.Pool, pos);
                Debug.Log($"[TileClickInput] 采集 grid={pos} ok={collected}");
            }
        }
    }
}
