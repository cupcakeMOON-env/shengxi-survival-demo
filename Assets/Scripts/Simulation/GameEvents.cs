using System;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 全局事件总线：模拟 → 表现的唯一通道。
    /// M0 提供地图与建筑事件，资源 / 天循环等事件随对应系统加入。
    /// </summary>
    public static class GameEvents
    {
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
        public static event Action<Enemy> EnemyDied;
        public static event Action<bool, int> GameOver;

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

        public static void RaiseEnemyDied(Enemy enemy) => EnemyDied?.Invoke(enemy);

        public static void RaiseGameOver(bool victory, int day) => GameOver?.Invoke(victory, day);
    }
}
