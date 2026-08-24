namespace ShengXi.Simulation
{
    /// <summary>
    /// 行动点：白天资源的核心节奏器。
    /// M2 先实现消耗与「结束回合」重置，白天/夜晚自动切换在 M3 接入。
    /// </summary>
    public class ActionPointSystem
    {
        public int MaxPerDay { get; }
        public int Current { get; private set; }

        public ActionPointSystem(int maxPerDay)
        {
            MaxPerDay = maxPerDay;
            Current = maxPerDay;
        }

        public bool CanSpend(int amount) => amount >= 0 && amount <= Current;

        public bool Spend(int amount)
        {
            if (!CanSpend(amount))
            {
                return false;
            }

            Current -= amount;
            GameEvents.RaiseActionPointsChanged(Current, MaxPerDay);
            return true;
        }

        public void ResetForNewDay()
        {
            Current = MaxPerDay;
            GameEvents.RaiseActionPointsChanged(Current, MaxPerDay);
        }

        /// <summary>读档恢复：直接设置当前行动点，不发事件。</summary>
        public void Restore(int current)
        {
            Current = System.Math.Max(0, System.Math.Min(current, MaxPerDay));
        }
    }
}
