using System.Collections.Generic;
using ShengXi.Simulation;
using UnityEngine;

namespace ShengXi.View
{
    /// <summary>
    /// 敌人表现：订阅敌人的生成/移动/死亡事件，用红色色块表现。
    /// </summary>
    public class EnemyView : MonoBehaviour
    {
        private readonly Dictionary<int, SpriteRenderer> _views = new Dictionary<int, SpriteRenderer>();
        private Sprite _sprite;
        private Transform _enemyRoot;

        private void OnEnable()
        {
            GameEvents.MapInitialized += OnMapInitialized;
            GameEvents.EnemySpawned += OnEnemySpawned;
            GameEvents.EnemyMoved += OnEnemyMoved;
            GameEvents.EnemyDied += OnEnemyDied;
            GameEvents.GameOver += OnGameOver;

            EnsureEnemyRoot();
        }

        private void OnDisable()
        {
            GameEvents.MapInitialized -= OnMapInitialized;
            GameEvents.EnemySpawned -= OnEnemySpawned;
            GameEvents.EnemyMoved -= OnEnemyMoved;
            GameEvents.EnemyDied -= OnEnemyDied;
            GameEvents.GameOver -= OnGameOver;
        }

        private void OnMapInitialized(GridMap map)
        {
            ClearAll();
        }

        private void OnEnemySpawned(Enemy enemy)
        {
            var go = new GameObject($"Enemy_{enemy.Id}");
            go.transform.SetParent(_enemyRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetSprite();
            sr.color = new Color(0.85f, 0.15f, 0.15f);
            sr.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            sr.transform.position = new Vector3(enemy.Position.X, enemy.Position.Y, -0.2f);
            _views[enemy.Id] = sr;
        }

        private void OnEnemyMoved(Enemy enemy)
        {
            if (_views.TryGetValue(enemy.Id, out var sr))
            {
                sr.transform.position = new Vector3(enemy.Position.X, enemy.Position.Y, -0.2f);
            }
        }

        private void OnEnemyDied(Enemy enemy)
        {
            if (_views.TryGetValue(enemy.Id, out var sr))
            {
                Destroy(sr.gameObject);
                _views.Remove(enemy.Id);
            }
        }

        private void OnGameOver(bool victory, int day)
        {
            ClearAll();
        }

        private void ClearAll()
        {
            if (_enemyRoot != null)
            {
                for (var i = _enemyRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(_enemyRoot.GetChild(i).gameObject);
                }
            }

            _views.Clear();
        }

        private void EnsureEnemyRoot()
        {
            if (_enemyRoot != null)
            {
                return;
            }

            var rootGo = new GameObject("Enemies");
            rootGo.transform.SetParent(transform, false);
            _enemyRoot = rootGo.transform;
        }

        private Sprite GetSprite()
        {
            if (_sprite == null)
            {
                _sprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0, 0, 1, 1),
                    new Vector2(0.5f, 0.5f),
                    1f);
            }

            return _sprite;
        }
    }
}
