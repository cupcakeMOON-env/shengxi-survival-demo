using ShengXi.Core;
using ShengXi.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ShengXi.View
{
    /// <summary>
    /// 开局据点选址：跟随鼠标显示金色（可放）/红色（不可放）预览，
    /// 左键点击可放置的格子直接落定据点（不再需要确认按钮）。
    /// </summary>
    public class BasePlacementController : MonoBehaviour
    {
        private Camera _camera;
        private SpriteRenderer _ghost;
        private bool _ghostCreated;

        public GridPos? Hovered { get; private set; }

        public void Initialize(Camera camera)
        {
            _camera = camera;
            CreateGhost();
        }

        private void Update()
        {
            var loop = GameLoop.Instance;
            if (loop == null || !loop.IsChoosingBase || _camera == null)
            {
                Hovered = null;
                if (_ghostCreated)
                {
                    _ghost.gameObject.SetActive(false);
                }

                return;
            }

            var world = _camera.ScreenToWorldPoint(Input.mousePosition);
            Hovered = new GridPos(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
            var pos = Hovered.Value;
            var canPlace = BasePlacementValidator.CanPlace(loop.Map, pos);

            _ghost.gameObject.SetActive(true);
            _ghost.transform.position = new Vector3(pos.X, pos.Y, -0.5f);
            _ghost.color = canPlace
                ? new Color(0.95f, 0.82f, 0.30f, 0.55f)
                : new Color(1f, 0f, 0f, 0.45f);

            if (canPlace &&
                Input.GetMouseButtonDown(0) &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                loop.ConfirmBase(pos);
            }
        }

        private void CreateGhost()
        {
            if (_ghostCreated)
            {
                return;
            }

            var go = new GameObject("BaseGhost");
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
