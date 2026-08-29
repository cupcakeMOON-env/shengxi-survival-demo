using System.Collections;
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
        private readonly Dictionary<int, Coroutine> _flashes = new Dictionary<int, Coroutine>();
        private const float FlashDuration = 0.18f;
        private const float DeathAnimDuration = 0.2f;
        private static readonly Color EnemyColor = new Color(0.85f, 0.15f, 0.15f);
        private Sprite _sprite;
        private Transform _enemyRoot;

        private void OnEnable()
        {
            GameEvents.MapInitialized += OnMapInitialized;
            GameEvents.EnemySpawned += OnEnemySpawned;
            GameEvents.EnemyMoved += OnEnemyMoved;
            GameEvents.EnemyDamaged += OnEnemyDamaged;
            GameEvents.EnemyDied += OnEnemyDied;
            GameEvents.GameOver += OnGameOver;

            EnsureEnemyRoot();
        }

        private void OnDisable()
        {
            GameEvents.MapInitialized -= OnMapInitialized;
            GameEvents.EnemySpawned -= OnEnemySpawned;
            GameEvents.EnemyMoved -= OnEnemyMoved;
            GameEvents.EnemyDamaged -= OnEnemyDamaged;
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
            sr.color = EnemyColor;
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

        private void OnEnemyDamaged(Enemy enemy)
        {
            if (!_views.TryGetValue(enemy.Id, out var sr))
            {
                return;
            }

            if (_flashes.TryGetValue(enemy.Id, out var running) && running != null)
            {
                StopCoroutine(running);
            }

            _flashes[enemy.Id] = StartCoroutine(FlashEnemy(sr, enemy.Id));
        }

        private void OnEnemyDied(Enemy enemy)
        {
            if (_views.TryGetValue(enemy.Id, out var sr))
            {
                _views.Remove(enemy.Id);
                StopFlash(enemy.Id);
                StartCoroutine(FadeOutEnemy(sr));
            }
        }

        private void OnGameOver(bool victory, int day)
        {
            ClearAll();
        }

        private void ClearAll()
        {
            foreach (var coroutine in _flashes.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }

            _flashes.Clear();
            if (_enemyRoot != null)
            {
                for (var i = _enemyRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(_enemyRoot.GetChild(i).gameObject);
                }
            }

            _views.Clear();
        }

        private IEnumerator FlashEnemy(SpriteRenderer sr, int enemyId)
        {
            var elapsed = 0f;
            while (elapsed < FlashDuration)
            {
                elapsed += Time.deltaTime;
                var pulse = 1f - Mathf.Abs(elapsed / FlashDuration * 2f - 1f);
                sr.color = Color.Lerp(EnemyColor, Color.white, pulse);
                yield return null;
            }

            if (sr != null)
            {
                sr.color = EnemyColor;
            }

            _flashes.Remove(enemyId);
        }

        private void StopFlash(int enemyId)
        {
            if (_flashes.TryGetValue(enemyId, out var running) && running != null)
            {
                StopCoroutine(running);
            }

            _flashes.Remove(enemyId);
        }

        /// <summary>死亡动画：0.2 秒内缩小并淡出后销毁（ClearAll 已销毁时安全退出）。</summary>
        private IEnumerator FadeOutEnemy(SpriteRenderer sr)
        {
            var startScale = sr.transform.localScale.x;
            var startColor = sr.color;
            var elapsed = 0f;
            while (elapsed < DeathAnimDuration)
            {
                if (sr == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var k = 1f - elapsed / DeathAnimDuration;
                sr.transform.localScale = new Vector3(startScale * k, startScale * k, 1f);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, k);
                yield return null;
            }

            if (sr != null)
            {
                Destroy(sr.gameObject);
            }
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
