using System;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 全局事件总线：模拟 → 表现的唯一通道。
    /// M0 提供地图与建筑事件，资源 / 天循环等事件随对应系统加入。
    /// </summary>
    public static class GameEvents
    {
        /// <summary>世界就绪（开新局 / 读档）；传 null 表示回到开始界面、世界已被清空。</summary>
        public static event Action<GridMap> MapInitialized;
        public static event Action<GridPos, TerrainType> TerrainChanged;
        public static event Action<GridPos, BuildingType> BuildingPlaced;
        public static event Action<GridPos> BuildingRemoved;
        public static event Action<ResourceType, int> ResourceChanged;
        public static event Action<int, int> ActionPointsChanged;
        public static event Action<DayPhase> PhaseChanged;
        public static event Action<int> DayChanged;
        public static event Action<int, int> BaseHpChanged;
        public static event Action<int> EnemyCountChanged;
        public static event Action<Enemy> EnemySpawned;
        public static event Action<Enemy> EnemyMoved;
        public static event Action<Enemy> EnemyDamaged;
        public static event Action<Enemy> EnemyDied;
        public static event Action<GridPos, int, int> BuildingDamaged;
        public static event Action<GridPos, int, int> BuildingRepaired;
        public static event Action<GridPos, bool> BuildingSupplyChanged;
        public static event Action<GridPos, BuildingType, int> BuildingUpgraded;
        public static event Action<GridPos> WorkshopStateChanged;
        /// <summary>选中 / 取消选中一座建筑（null 表示取消选中）。各面板自己判断认不认这座建筑。</summary>
        public static event Action<GridPos?> BuildingSelected;
        public static event Action<RunOutcome, int> GameOver;

        public static void RaiseMapInitialized(GridMap map) => MapInitialized?.Invoke(map);

        public static void RaiseTerrainChanged(GridPos pos, TerrainType terrain) => TerrainChanged?.Invoke(pos, terrain);

        public static void RaiseBuildingPlaced(GridPos pos, BuildingType building) => BuildingPlaced?.Invoke(pos, building);

        public static void RaiseBuildingRemoved(GridPos pos) => BuildingRemoved?.Invoke(pos);

        public static void RaiseResourceChanged(ResourceType type, int amount) => ResourceChanged?.Invoke(type, amount);

        public static void RaiseActionPointsChanged(int current, int max) => ActionPointsChanged?.Invoke(current, max);

        public static void RaisePhaseChanged(DayPhase phase) => PhaseChanged?.Invoke(phase);

        public static void RaiseDayChanged(int day) => DayChanged?.Invoke(day);

        public static void RaiseBaseHpChanged(int current, int max) => BaseHpChanged?.Invoke(current, max);

        public static void RaiseEnemyCountChanged(int count) => EnemyCountChanged?.Invoke(count);

        public static void RaiseEnemySpawned(Enemy enemy) => EnemySpawned?.Invoke(enemy);

        public static void RaiseEnemyMoved(Enemy enemy) => EnemyMoved?.Invoke(enemy);

        public static void RaiseEnemyDamaged(Enemy enemy) => EnemyDamaged?.Invoke(enemy);

        public static void RaiseEnemyDied(Enemy enemy) => EnemyDied?.Invoke(enemy);

        public static void RaiseBuildingDamaged(GridPos pos, int currentHp, int maxHp) => BuildingDamaged?.Invoke(pos, currentHp, maxHp);

        public static void RaiseBuildingRepaired(GridPos pos, int currentHp, int maxHp) => BuildingRepaired?.Invoke(pos, currentHp, maxHp);

        public static void RaiseBuildingSupplyChanged(GridPos pos, bool supplied) => BuildingSupplyChanged?.Invoke(pos, supplied);

        public static void RaiseBuildingUpgraded(GridPos pos, BuildingType building, int level) =>
            BuildingUpgraded?.Invoke(pos, building, level);

        /// <summary>某座工坊的开工状态或配方开关发生变化。</summary>
        public static void RaiseWorkshopStateChanged(GridPos pos) => WorkshopStateChanged?.Invoke(pos);

        /// <summary>选中 / 取消选中一座建筑。纯视图状态，不进存档。</summary>
        public static void RaiseBuildingSelected(GridPos? pos) => BuildingSelected?.Invoke(pos);

        /// <summary>一局结束（胜利 / 失败 / 主动结算），day 的含义随结局不同，见 GameOverText。</summary>
        public static void RaiseGameOver(RunOutcome outcome, int day) => GameOver?.Invoke(outcome, day);
    }
}
