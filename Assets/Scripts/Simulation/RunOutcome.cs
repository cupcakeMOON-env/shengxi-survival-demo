namespace ShengXi.Simulation
{
    /// <summary>
    /// 一局游戏的结束方式。
    /// 用枚举而不是 bool：结局不止胜负两种——玩家还可以自己提前收局（Settled）。
    /// </summary>
    public enum RunOutcome
    {
        /// <summary>撑过 WaveScheduler.WinDay 天。</summary>
        Victory = 0,

        /// <summary>据点被摧毁。</summary>
        Defeat = 1,

        /// <summary>玩家主动结算，提前收局。</summary>
        Settled = 2,
    }
}
