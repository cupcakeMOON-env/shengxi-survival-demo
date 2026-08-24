namespace ShengXi.Simulation
{
    /// <summary>
    /// 敌人：纯数据，位置由 CombatSim 推进，到达据点后造成伤害。
    /// </summary>
    public class Enemy
    {
        public int Id { get; }
        public GridPos Position { get; set; }
        public int HP { get; set; }
        public int MaxHp { get; }
        public int DamageToBase { get; }

        public bool IsDead => HP <= 0;

        public Enemy(int id, GridPos position, int maxHp, int damageToBase)
        {
            Id = id;
            Position = position;
            MaxHp = maxHp;
            HP = maxHp;
            DamageToBase = damageToBase;
        }
    }
}
