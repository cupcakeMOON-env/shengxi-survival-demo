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

        /// <summary>本局的结束方式；只有 GameOver 为 true 时才有值（未结束时为 null）。</summary>
        public RunOutcome? Outcome { get; private set; }

        /// <summary>是否停在开始界面。开机即为 true，开始新游戏 / 读档后才变成 false。</summary>
        public bool IsInMenu { get; private set; } = true;

        /// <summary>当前是否已经有一局世界在跑。开始界面阶段 Map 等模拟数据都是 null。</summary>
        public bool HasWorld => Map != null;

        /// <summary>最近一次存 / 读档用的槽位，默认第一个存档位。</summary>
        public string CurrentSlot { get; private set; } = SaveSlots.IdAt(0);

        /// <summary>临时暂停模拟（打开存档面板等模态界面时用），不改变胜负状态。</summary>
        public bool IsPaused { get; private set; }
        public int Seed { get; private set; }
        private bool _baseConfirmed;

        /// <summary>开局是否处于「选择据点位置」阶段。</summary>
        public bool IsChoosingBase => Map != null && !_baseConfirmed;

        /// <summary>
        /// 开新局：原地重置全部状态（不重载场景），并通过事件让所有视图重建。
        /// 供「重新开始」与 PlayMode 测试复用。
        /// </summary>
        public void NewGame()
        {
            IsInMenu = false;
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
            Outcome = null;
            _baseConfirmed = false;
            _nextEnemyId = 0;
            _collectorTimer = 0f;
            _combatTimer = 0f;
            _supplyTimer = 0f;
            _productionTimer = 0f;

            GameEvents.RaiseMapInitialized(Map);
            GameEvents.RaiseResourceChanged(ResourceType.Wood, 0);
            GameEvents.RaiseResourceChanged(ResourceType.Stone, 0);
            GameEvents.RaiseResourceChanged(ResourceType.Food, 0);
            GameEvents.RaiseResourceChanged(ResourceType.Material, 0);
            GameEvents.RaiseResourceChanged(ResourceType.RepairKit, 0);
            GameEvents.RaiseActionPointsChanged(ActionPoints.Current, ActionPoints.MaxPerDay);
            GameEvents.RaiseDayChanged(Cycle.Day);
            GameEvents.RaisePhaseChanged(Cycle.Phase);
            GameEvents.RaiseEnemyCountChanged(0);
        }

        private const float CollectorTickInterval = 1f;
        private float _collectorTimer;
        private const float CombatTickInterval = 0.5f;
        private float _combatTimer;
        private const float SupplyTickInterval = FoodSupplySystem.TickInterval;
        private float _supplyTimer;
        private const float ProductionTickInterval = ProductionSystem.TickInterval;
        private float _productionTimer;
        private int _nextEnemyId;
        private const int StartingWood = 20;
        private const int StartingStone = 15;

        private void Awake()
        {
            // 开机先进开始界面：地图 / 资源 / 日夜这些模拟数据由 NewGame / LoadGame 创建，
            // 这里只登记实例，不生成任何世界，避免"菜单背后其实已经跑着一局"。
            Instance = this;
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
            CurrentSlot = slot;
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
            IsInMenu = false;
            CurrentSlot = slot;
            Seed = data.seed;
            Map = SaveSerializer.RebuildMap(data);
            Pool = SaveSerializer.RebuildPool(data);
            ActionPoints = SaveSerializer.RebuildActionPoints(data);
            Cycle = SaveSerializer.RebuildCycle(data);
            Base = SaveSerializer.RebuildBase(data);
            Enemies = SaveSerializer.RebuildEnemies(data);
            _baseConfirmed = true;
            GameOver = false;
            Outcome = null;
            _nextEnemyId = 0;
            _supplyTimer = 0f;
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

            // 夜晚读档：按当前食物储备重算断粮状态，让视图与开火判定从第一帧就正确
            if (data.isNight)
            {
                FoodSupplySystem.Refresh(Map, Pool);
            }

            // 统一刷新：视图层全部通过事件重建
            GameEvents.RaiseMapInitialized(Map);
            GameEvents.RaiseResourceChanged(ResourceType.Wood, Pool.GetAmount(ResourceType.Wood));
            GameEvents.RaiseResourceChanged(ResourceType.Stone, Pool.GetAmount(ResourceType.Stone));
            GameEvents.RaiseResourceChanged(ResourceType.Food, Pool.GetAmount(ResourceType.Food));
            GameEvents.RaiseResourceChanged(ResourceType.Material, Pool.GetAmount(ResourceType.Material));
            GameEvents.RaiseResourceChanged(ResourceType.RepairKit, Pool.GetAmount(ResourceType.RepairKit));
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
            // 夜晚开始：先按储备刷新一次供给（不扣费），没粮的箭塔立刻停火
            FoodSupplySystem.Refresh(Map, Pool);
            _combatTimer = 0f;
            _supplyTimer = 0f;
        }

        /// <summary>
        /// 主动结算：玩家提前收局，按当前局面出结算页。
        /// 已经结束（胜负已分或已结算）或还没落据点时返回 false，避免重复结算把局面又"清"一遍。
        /// 白天夜晚都允许——夜里打不下去想收手，也应该能收。
        /// </summary>
        public bool SettleRun()
        {
            if (GameOver || !_baseConfirmed)
            {
                return false;
            }

            GameOver = true;
            Outcome = RunOutcome.Settled;
            GameEvents.RaiseGameOver(RunOutcome.Settled, Cycle.Day);
            return true;
        }

        /// <summary>把当前局面拍成一份结算数据，供结算页渲染。</summary>
        public RunSummary BuildSummary(RunOutcome outcome) =>
            SettlementService.Build(Map, Pool, ActionPoints, Cycle, Base, outcome);

        /// <summary>
        /// 回到开始界面：清空当前世界并广播 MapInitialized(null)，
        /// 让所有视图把画面收干净（开始界面正是靠这个事件显示自己的）。
        /// 不要在开始界面状态调用——那时本来就没有世界。
        /// </summary>
        public void ReturnToMenu()
        {
            IsInMenu = true;
            IsPaused = false;
            GameOver = false;
            Outcome = null;
            _baseConfirmed = false;
            Map = null;
            Pool = null;
            ActionPoints = null;
            Cycle = null;
            Base = null;
            Enemies = new List<Enemy>();
            _collectorTimer = 0f;
            _combatTimer = 0f;
            _supplyTimer = 0f;
            _productionTimer = 0f;

            GameEvents.RaiseMapInitialized(null);
            GameEvents.RaiseEnemyCountChanged(0);
        }

        /// <summary>暂停 / 恢复模拟推进。开始界面与已结束的局面本来就不推进，这里只管模态界面。</summary>
        public void SetPaused(bool paused) => IsPaused = paused;

        private void Update()
        {
            // 开始界面（没有世界）、模态界面暂停、本局已结束都不推进模拟
            if (IsInMenu || IsPaused || GameOver || Map == null)
            {
                return;
            }

            _collectorTimer += Time.deltaTime;
            if (_collectorTimer >= CollectorTickInterval)
            {
                _collectorTimer = 0f;
                CollectorSystem.Tick(Map, Pool);
            }

            // 工坊只在白天生产：夜晚人力用于防守，玩家无法再补原料/产能
            if (Cycle.IsDay)
            {
                _productionTimer += Time.deltaTime;
                if (_productionTimer >= ProductionTickInterval)
                {
                    _productionTimer = 0f;
                    ProductionSystem.Tick(Map, Pool);
                }
            }

            if (Cycle.IsNight && Enemies.Count > 0)
            {
                _supplyTimer += Time.deltaTime;
                if (_supplyTimer >= SupplyTickInterval)
                {
                    _supplyTimer = 0f;
                    FoodSupplySystem.Tick(Map, Pool);
                }

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
            CombatSim.Tick(Map, Pool, Base, Enemies);
            GameEvents.RaiseEnemyCountChanged(Enemies.Count);

            if (Base.IsDestroyed)
            {
                GameOver = true;
                Outcome = RunOutcome.Defeat;
                GameEvents.RaiseGameOver(RunOutcome.Defeat, Cycle.Day);
                return;
            }

            if (Enemies.Count == 0)
            {
                Cycle.EndNight();
                ActionPoints.ResetForNewDay();
                FoodSupplySystem.RestoreAll(Map);
                if (Cycle.Day > WaveScheduler.WinDay)
                {
                    GameOver = true;
                    Outcome = RunOutcome.Victory;
                    GameEvents.RaiseGameOver(RunOutcome.Victory, Cycle.Day);
                }
            }
        }
    }
}
