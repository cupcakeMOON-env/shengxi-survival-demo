namespace ShengXi.Simulation
{
    /// <summary>
    /// 建筑定义：纯数据，不依赖 Unity。
    /// M2 用代码目录（BuildingCatalog）创建实例，后续可迁移为 ScriptableObject 资产。
    /// </summary>
    public class BuildingDef
    {
        public BuildingType Type;
        public string Name;
        public int WoodCost;
        public int StoneCost;
        public int FoodCost;
        public int ActionPointCost;
        public int CapacityBonus;          // 仓库：提升资源容量上限
        public int Range;                  // 攻击类建筑：射程（曼哈顿距离，单位=格）
        public int Damage;                 // 攻击类建筑：每 tick 伤害
        public bool RequiresResourceTile;  // 采集站：必须建在有剩余资源的资源格上
        public string Description;
    }
}
