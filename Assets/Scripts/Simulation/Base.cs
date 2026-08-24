using System;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 据点：地图中央，夜晚被敌人攻击，血量归零即失败。
    /// </summary>
    public class Base
    {
        public GridPos Position { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public bool IsDestroyed => CurrentHp <= 0;

        public Base(GridPos position, int maxHp)
        {
            Position = position;
            MaxHp = maxHp;
            CurrentHp = maxHp;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDestroyed)
            {
                return;
            }

            CurrentHp = Math.Max(0, CurrentHp - amount);
            GameEvents.RaiseBaseHpChanged(CurrentHp, MaxHp);
        }

        /// <summary>读档恢复：直接设置血量，不发事件。</summary>
        public void Restore(int hp)
        {
            CurrentHp = Math.Max(0, Math.Min(hp, MaxHp));
        }
    }
}
