using ShengXi.Core;
using ShengXi.Simulation;
using UnityEngine;

namespace ShengXi.View
{
    /// <summary>
    /// 建造/拆除/升级/修理模式：持有当前选中的建筑定义（或某个功能标记），
    /// 鼠标悬停时在目标格显示红/绿幽灵预览。
    /// 绿色=该操作可行，红色=不可行（资源/行动点不足、目标非法等）。
    /// </summary>
    public class BuildModeController : MonoBehaviour
    {
        private Camera _camera;
        private SpriteRenderer _ghost;
        private bool _ghostCreated;

        public BuildingDef ActiveDef { get; private set; }
        public bool IsDemolishing { get; private set; }
        public bool IsRepairing { get; private set; }

        /// <summary>当前是否处于某个操作模式（建造 / 拆除 / 修理）。</summary>
        public bool HasActiveMode => ActiveDef != null || IsDemolishing || IsRepairing;

        public void Initialize(Camera camera)
        {
            _camera = camera;
            CreateGhost();
        }

        public void SetActive(BuildingDef def)
        {
            var toggled = def != null && def == ActiveDef ? null : def;
            ActiveDef = toggled;
            if (toggled != null)
            {
                IsDemolishing = false;
                IsRepairing = false;
            }
        }

        /// <summary>切换拆除模式：再次点击关闭，切换建造时自动退出。</summary>
        public void SetDemolishActive()
        {
            IsDemolishing = !IsDemolishing;
            if (IsDemolishing)
            {
                ActiveDef = null;
                IsRepairing = false;
            }
        }

        /// <summary>切换修理模式：再次点击关闭，切建造/拆除时自动退出。</summary>
        public void SetRepairActive()
        {
            IsRepairing = !IsRepairing;
            if (IsRepairing)
            {
                ActiveDef = null;
                IsDemolishing = false;
            }
        }

        public void ClearActive()
        {
            ActiveDef = null;
            IsDemolishing = false;
            IsRepairing = false;
        }

        /// <summary>
        /// 玩家主动退出当前操作模式（右键）。
        /// 与 ClearActive 等价，单独留一个入口是为了把「玩家取消」和「切换模式时的内部清理」
        /// 在语义上分开，也方便在这里加日志 / 音效 / 提示。
        /// </summary>
        public void CancelActiveMode()
        {
            if (!HasActiveMode)
            {
                return;
            }

            ClearActive();
            Debug.Log("[BuildModeController] 右键退出当前操作模式");
        }

        private void Update()
        {
            // 右键退出当前操作模式，等价于再点一次那个按钮。
            // 放在最前面：不管白天黑夜、也不管鼠标是不是压在 UI 上，都能取消。
            if (HasActiveMode && Input.GetMouseButtonDown(1))
            {
                CancelActiveMode();
                return;
            }

            if (GameLoop.Instance == null ||
                !GameLoop.Instance.HasWorld ||
                GameLoop.Instance.GameOver ||
                !GameLoop.Instance.Cycle.IsDay ||
                GameLoop.Instance.IsChoosingBase ||
                _camera == null ||
                !HasActiveMode)
            {
                if (_ghostCreated)
                {
                    _ghost.gameObject.SetActive(false);
                }

            return;
        }

            var picked = GridPicking.ScreenToGrid(_camera, Input.mousePosition);
            if (!picked.HasValue)
            {
                _ghost.gameObject.SetActive(false);
                return;
            }

            var pos = picked.Value;

            if (IsDemolishing)
            {
                var tile = GameLoop.Instance.Map.GetTile(pos);
                if (tile == null || tile.Building == BuildingType.None)
                {
                    _ghost.gameObject.SetActive(false);
                    return;
                }

                _ghost.gameObject.SetActive(true);
                _ghost.transform.position = new Vector3(pos.X, pos.Y, -0.5f);
                _ghost.color = DemolishService.CanDemolish(GameLoop.Instance.Map, pos)
                    ? new Color(0f, 1f, 0f, 0.45f)
                    : new Color(1f, 0f, 0f, 0.45f);
                return;
            }

            if (IsRepairing)
            {
                var baseDefense = GameLoop.Instance.Base;
                if (baseDefense != null && baseDefense.Position == pos)
                {
                    _ghost.gameObject.SetActive(true);
                    _ghost.transform.position = new Vector3(pos.X, pos.Y, -0.5f);
                    _ghost.color = RepairService.CanRepairBase(
                        baseDefense,
                        GameLoop.Instance.Pool,
                        GameLoop.Instance.ActionPoints)
                        ? new Color(0f, 1f, 0f, 0.45f)
                        : new Color(1f, 0f, 0f, 0.45f);
                    return;
                }

                var repairTile = GameLoop.Instance.Map.GetTile(pos);
                if (repairTile == null || repairTile.Building == BuildingType.None)
                {
                    _ghost.gameObject.SetActive(false);
                    return;
                }

                _ghost.gameObject.SetActive(true);
                _ghost.transform.position = new Vector3(pos.X, pos.Y, -0.5f);
                _ghost.color = RepairService.CanRepairBuilding(
                    GameLoop.Instance.Map,
                    GameLoop.Instance.Pool,
                    GameLoop.Instance.ActionPoints,
                    pos)
                    ? new Color(0f, 1f, 0f, 0.45f)
                    : new Color(1f, 0f, 0f, 0.45f);
                return;
            }

            _ghost.gameObject.SetActive(true);
            _ghost.transform.position = new Vector3(pos.X, pos.Y, -0.5f);
            _ghost.color = BuildService.CanBuild(
                GameLoop.Instance.Map,
                GameLoop.Instance.Pool,
                GameLoop.Instance.ActionPoints,
                ActiveDef,
                pos)
                ? new Color(0f, 1f, 0f, 0.45f)
                : new Color(1f, 0f, 0f, 0.45f);
        }

        private void CreateGhost()
        {
            if (_ghostCreated)
            {
                return;
            }

            var go = new GameObject("GhostPreview");
            go.transform.SetParent(transform, false);
            _ghost = go.AddComponent<SpriteRenderer>();
            _ghost.sprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f),
                1f);
            _ghost.transform.localScale = new Vector3(0.96f, 0.96f, 1f);
            _ghost.gameObject.SetActive(false);
            _ghostCreated = true;
        }
    }
}
