namespace ShengXi.Simulation
{
    /// <summary>
    /// 敌人：纯数据，位置由 CombatSim 推进；伤害作用于建筑或据点。
    /// </summary>
    public class Enemy
    {
        public int Id { get; }
        public GridPos Position { get; set; }
        public int HP { get; set; }
        public int MaxHp { get; }
        public int Damage { get; }

        public bool IsDead => HP <= 0;

        public Enemy(int id, GridPos position, int maxHp, int damage)
        {
            Id = id;
            Position = position;
            MaxHp = maxHp;
            HP = maxHp;
            Damage = damage;
        }
    }
}
