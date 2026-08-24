namespace ShengXi.Simulation
{
    public enum TerrainType
    {
        Grass = 0,
        Forest = 1,
        Stone = 2,
        Water = 3,
        Bush = 4,
    }

    public enum BuildingType
    {
        None = 0,
        Collector = 1,   // 采集站
        Warehouse = 2,   // 仓库
        Wall = 3,        // 围墙
        ArrowTower = 4,  // 箭塔
        Workshop = 5,    // 工坊
        Base = 6,        // 据点
    }

    /// <summary>单个格子的数据：地形、资源余量、建筑、可通行性。</summary>
    public class Tile
    {
        public TerrainType Terrain { get; set; }
        public int ResourceAmount { get; set; }
        public BuildingType Building { get; set; }

        /// <summary>敌人/单位能否通过（水面与围墙不可通行）。</summary>
        public bool IsWalkable => Terrain != TerrainType.Water && Building != BuildingType.Wall;

        public Tile(TerrainType terrain)
        {
            Terrain = terrain;
            ResourceAmount = 0;
            Building = BuildingType.None;
        }
    }
}
