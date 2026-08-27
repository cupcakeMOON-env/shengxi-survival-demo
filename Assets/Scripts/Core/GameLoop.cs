using System.Collections.Generic;
using ShengXi.Simulation;
using UnityEngine;

namespace ShengXi.Core
{
    /// <summary>
    /// 模拟循环宿主：持有模拟世界并驱动 tick。
    /// M0 只负责初始化地图；采集、行动点等系统的 Tick 从 M1 起接入 Update。
    /// </summary>
    public class GameLoop : MonoBehaviour
    {
        public static GameLoop Instance { get; private set; }

        public GridMap Map { get; private set; }
        public ResourcePool Pool { get; private set; }
        public ActionPointSystem ActionPoints { get; private set; }
        public DayCycle Cycle { get; private set; }
        public Base Base { get; private set; }
        public List<Enemy> Enemies { get; private set; } = new List<Enemy>();
        public bool GameOver { get; private set; }
        public bool Victory { get; private set; }
        public int Seed { get; private set; }
        private bool _baseConfirmed;

        /// <summary>开局是否处于「选择据点位置」阶段。</summary>
        public bool IsChoosingBase => !_baseConfirmed;

        /// <summary>
        /// 开新局：原地重置全部状态（不重载场景），并通过事件让所有视图重建。
        /// 供「重新开始」与 PlayMode 测试复用。
        /// </summary>
        public void NewGame()
        {
            Seed = Random.Range(1, int.MaxValue);
            Map = MapGenerator.CreateRandomMap(MapGenerator.DefaultWidth, MapGenerator.DefaultHeight, Seed);
            Pool = new ResourcePool();
            Pool.Add(ResourceType.Wood, StartingWood);
            Pool.Add(ResourceType.Stone, StartingStone);
            ActionPoints = new ActionPointSystem(10);
            Cycle = new DayCycle();
            Base = null;
            Enemies = new List<Enemy>();
            GameOver = false;
            Victory = false;
            _baseConfirmed = false;
            _nextEnemyId = 0;
            _collectorTimer = 0f;
            _combatTimer = 0f;

            GameEvents.RaiseMapInitialized(Map);
            GameEvents.RaiseResourceChanged(ResourceType.Wood, 0);
            GameEvents.RaiseResourceChanged(ResourceType.Stone, 0);
            GameEvents.RaiseResourceChanged(ResourceType.Food, 0);
            GameEvents.RaiseActionPointsChanged(ActionPoints.Current, ActionPoints.MaxPerDay);
            GameEvents.RaiseDayChanged(Cycle.Day);
            GameEvents.RaisePhaseChanged(Cycle.Phase);
            GameEvents.RaiseEnemyCountChanged(0);
        }

        private const float CollectorTickInterval = 1f;
        private float _collectorTimer;
        private const float CombatTickInterval = 0.5f;
        private float _combatTimer;
        private int _nextEnemyId;
        private const int StartingWood = 20;
        private const int StartingStone = 15;

        private void Awake()
        {
            Instance = this;
            Seed = Random.Range(1, int.MaxValue);
            Map = MapGenerator.CreateRandomMap(MapGenerator.DefaultWidth, MapGenerator.DefaultHeight, Seed);
            Pool = new ResourcePool();
            Pool.Add(ResourceType.Wood, StartingWood);
            Pool.Add(ResourceType.Stone, StartingStone);
            ActionPoints = new ActionPointSystem(10);
            Cycle = new DayCycle();
            GameEvents.RaiseMapInitialized(Map);
            GameEvents.RaiseDayChanged(Cycle.Day);
            GameEvents.RaisePhaseChanged(Cycle.Phase);
            GameEvents.RaiseActionPointsChanged(ActionPoints.Current, ActionPoints.MaxPerDay);
        }

        /// <summary>
        /// 确认据点位置：校验 → 清地基 → 放置 → 进入正式游戏。
        /// </summary>
        public bool ConfirmBase(GridPos pos)
        {
            if (_baseConfirmed || !BasePlacementValidator.CanPlace(Map, pos))
            {
                return false;
            }

            Map.SetTerrain(pos, TerrainType.Grass, 0);
            Map.Place(BuildingType.Base, pos);
            Base = new Base(pos, 20);
            _baseConfirmed = true;
            GameEvents.RaiseBuildingPlaced(pos, BuildingType.Base);
            GameEvents.RaiseBaseHpChanged(Base.CurrentHp, Base.MaxHp);
            return true;
        }

        /// <summary>存档：打包当前状态写入文件。</summary>
        public void SaveGame(string slot = "shengxi_save.json")
        {
            if (!_baseConfirmed)
            {
                Debug.LogWarning("[Save] 还未放置据点，无法存档");
                return;
            }

            var data = SaveSerializer.Build(Map, Pool, ActionPoints, Cycle, Base, Enemies, Seed);
            SaveMigrator.Upgrade(data);
            SaveService.Write(data, slot);
            Debug.Log($"[Save] 已存档：第 {Cycle.Day} 天");
        }

        /// <summary>读档：从文件还原状态并统一刷新所有表现层。</summary>
        public void LoadGame(string slot = "shengxi_save.json")
        {
            var data = SaveService.Read(slot);
            if (data == null)
            {
                Debug.LogWarning("[Save] 没有找到存档");
                return;
            }

            data = SaveMigrator.Upgrade(data);
            Seed = data.seed;
            Map = SaveSerializer.RebuildMap(data);
            Pool = SaveSerializer.RebuildPool(data);
            ActionPoints = SaveSerializer.RebuildActionPoints(data);
            Cycle = SaveSerializer.RebuildCycle(data);
            Base = SaveSerializer.RebuildBase(data);
            Enemies = SaveSerializer.RebuildEnemies(data);
            _baseConfirmed = true;
            GameOver = false;
            Victory = false;
            _nextEnemyId = 0;
            if (data.enemies != null)
            {
                foreach (var enemy in data.enemies)
                {
                    if (enemy.id + 1 > _nextEnemyId)
                    {
                        _nextEnemyId = enemy.id + 1;
                    }
                }
            }

            // 统一刷新：视图层全部通过事件重建
            GameEvents.RaiseMapInitialized(Map);
            GameEvents.RaiseResourceChanged(ResourceType.Wood, Pool.GetAmount(ResourceType.Wood));
            GameEvents.RaiseResourceChanged(ResourceType.Stone, Pool.GetAmount(ResourceType.Stone));
            GameEvents.RaiseResourceChanged(ResourceType.Food, Pool.GetAmount(ResourceType.Food));
            GameEvents.RaiseActionPointsChanged(ActionPoints.Current, ActionPoints.MaxPerDay);
            GameEvents.RaiseDayChanged(Cycle.Day);
            GameEvents.RaisePhaseChanged(Cycle.Phase);
            GameEvents.RaiseBaseHpChanged(Base.CurrentHp, Base.MaxHp);
            foreach (var enemy in Enemies)
            {
                GameEvents.RaiseEnemySpawned(enemy);
            }

            GameEvents.RaiseEnemyCountChanged(Enemies.Count);
            Debug.Log($"[Save] 读档完成：第 {Cycle.Day} 天");
        }

        /// <summary>
        /// 结束白天：进入夜晚、刷出当晚波次（UI 按钮调用）。
        /// </summary>
        public void EndDay()
        {
            if (GameOver || !_baseConfirmed || !Cycle.IsDay)
            {
                return;
            }

            if (!Cycle.StartNight())
            {
                return;
            }

            var spawnPoints = WaveScheduler.SpawnPointsFor(Map, Base.Position);
            var wave = WaveScheduler.SpawnWave(Map, Cycle.Day, _nextEnemyId, spawnPoints);
            _nextEnemyId += wave.Count;
            foreach (var enemy in wave)
            {
                GameEvents.RaiseEnemySpawned(enemy);
            }

            Enemies = wave;
            GameEvents.RaiseEnemyCountChanged(Enemies.Count);
            _combatTimer = 0f;
        }

        private void Update()
        {
            if (GameOver)
            {
                return;
            }

            _collectorTimer += Time.deltaTime;
            if (_collectorTimer >= CollectorTickInterval)
            {
                _collectorTimer = 0f;
                CollectorSystem.Tick(Map, Pool);
            }

            if (Cycle.IsNight && Enemies.Count > 0)
            {
                _combatTimer += Time.deltaTime;
                if (_combatTimer >= CombatTickInterval)
                {
                    _combatTimer = 0f;
                    TickNight();
                }
            }
        }

        private void TickNight()
        {
            CombatSim.Tick(Map, Base, Enemies);
            GameEvents.RaiseEnemyCountChanged(Enemies.Count);

            if (Base.IsDestroyed)
            {
                GameOver = true;
                Victory = false;
                GameEvents.RaiseGameOver(false, Cycle.Day);
                return;
            }

            if (Enemies.Count == 0)
            {
                Cycle.EndNight();
                ActionPoints.ResetForNewDay();
                if (Cycle.Day > WaveScheduler.WinDay)
                {
                    GameOver = true;
                    Victory = true;
                    GameEvents.RaiseGameOver(true, Cycle.Day);
                }
            }
        }
    }
}
