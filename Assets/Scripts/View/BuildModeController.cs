using ShengXi.Core;
using ShengXi.Simulation;
using UnityEngine;

namespace ShengXi.View
{
    /// <summary>
    /// 建造/拆除模式：持有当前选中的建筑定义（或拆除标记），鼠标悬停时在目标格显示红/绿幽灵预览。
    /// 绿色=可以建造/拆除，红色=不能（建造：资源不足/行动点不足/位置非法；拆除：据点不可拆）。
    /// </summary>
    public class BuildModeController : MonoBehaviour
    {
        private Camera _camera;
        private SpriteRenderer _ghost;
        private bool _ghostCreated;

        public BuildingDef ActiveDef { get; private set; }
        public bool IsDemolishing { get; private set; }
        public bool IsUpgrading { get; private set; }

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
                IsUpgrading = false;
            }
        }

        /// <summary>切换拆除模式：再次点击关闭，切换建造时自动退出。</summary>
        public void SetDemolishActive()
        {
            IsDemolishing = !IsDemolishing;
            if (IsDemolishing)
            {
                ActiveDef = null;
                IsUpgrading = false;
            }
        }

        /// <summary>切换升级模式：再次点击关闭，切建造/拆除时自动退出。</summary>
        public void SetUpgradeActive()
        {
            IsUpgrading = !IsUpgrading;
            if (IsUpgrading)
            {
                ActiveDef = null;
                IsDemolishing = false;
            }
        }

        public void ClearActive()
        {
            ActiveDef = null;
            IsDemolishing = false;
            IsUpgrading = false;
        }

        private void Update()
        {
            if (GameLoop.Instance == null ||
                GameLoop.Instance.GameOver ||
                !GameLoop.Instance.Cycle.IsDay ||
                GameLoop.Instance.IsChoosingBase ||
                _camera == null ||
                (ActiveDef == null && !IsDemolishing && !IsUpgrading))
            {
                if (_ghostCreated)
                {
                    _ghost.gameObject.SetActive(false);
                }

                return;
            }

            var world = _camera.ScreenToWorldPoint(Input.mousePosition);
            var pos = new GridPos(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));

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

            if (IsUpgrading)
            {
                _ghost.gameObject.SetActive(true);
                _ghost.transform.position = new Vector3(pos.X, pos.Y, -0.5f);
                _ghost.color = UpgradeService.CanUpgrade(
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
