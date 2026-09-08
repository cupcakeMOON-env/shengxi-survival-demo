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
        /// <summary>建筑当前血量；无建筑时为 0。敌人攻击建筑时扣减，归零即摧毁。</summary>
        public int BuildingHp { get; set; }
        /// <summary>
        /// 防御建筑是否因食物耗尽而断粮停火（瞬态：不存档，读档后由 FoodSupplySystem 重算）。
        /// </summary>
        public bool BuildingStarved { get; set; }

        /// <summary>
        /// 敌人/单位能否通过（水面与建筑不可通行；据点例外，敌人可踏入据点发动攻击）。
        /// 建筑不可通行后，敌人只能贴着建筑攻击，符合塔防「先拆防御再打据点」的节奏。
        /// </summary>
        public bool IsWalkable => Terrain != TerrainType.Water &&
                                 (Building == BuildingType.None || Building == BuildingType.Base);

        public Tile(TerrainType terrain)
        {
            Terrain = terrain;
            ResourceAmount = 0;
            Building = BuildingType.None;
        }
    }
}
