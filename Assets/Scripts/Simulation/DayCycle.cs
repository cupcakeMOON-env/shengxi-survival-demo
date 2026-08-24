namespace ShengXi.Simulation
{
    public enum DayPhase
    {
        Day = 0,
        Night = 1,
    }

    /// <summary>
    /// 日夜循环状态机：白天行动、夜晚战斗，夜晚结束天数 +1。
    /// </summary>
    public class DayCycle
    {
        public int Day { get; private set; } = 1;
        public DayPhase Phase { get; private set; } = DayPhase.Day;

        public bool IsDay => Phase == DayPhase.Day;
        public bool IsNight => Phase == DayPhase.Night;

        public bool StartNight()
        {
            if (Phase != DayPhase.Day)
            {
                return false;
            }

            Phase = DayPhase.Night;
            GameEvents.RaisePhaseChanged(Phase);
            return true;
        }

        public bool EndNight()
        {
            if (Phase != DayPhase.Night)
            {
                return false;
            }

            Phase = DayPhase.Day;
            Day++;
            GameEvents.RaisePhaseChanged(Phase);
            GameEvents.RaiseDayChanged(Day);
            return true;
        }

        /// <summary>读档恢复：直接设置状态，不发事件（由上层在恢复完成后统一刷新）。</summary>
        public void Restore(int day, bool isNight)
        {
            Day = day >= 1 ? day : 1;
            Phase = isNight ? DayPhase.Night : DayPhase.Day;
        }
    }
}
