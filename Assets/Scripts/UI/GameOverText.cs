using System.Collections.Generic;
using ShengXi.Simulation;
using UnityEngine;

namespace ShengXi.UI
{
    /// <summary>
    /// 结算文案与配色的唯一出处。
    ///
    /// 为什么天数要减一：胜利判定发生在 <c>DayCycle.EndNight()</c> 之后
    /// （第 7 天夜晚清场 → 天数自增到 8 → 8 &gt; WinDay 判胜），
    /// 所以 GameOver 事件带过来的 day 是「已经进入的新一天」，不是玩家实际撑过的天数。
    /// 顶部横幅和结算面板都从这里取文案，避免两个界面各写一份、算出两个数字。
    /// </summary>
    public static class GameOverText
    {
        /// <summary>玩家实际撑过的天数（失败时未过夜，不走这个换算）。</summary>
        public static int SurvivedDays(int day) => day > 0 ? day - 1 : 0;

        /// <summary>结算标题。</summary>
        public static string Headline(RunOutcome outcome) => outcome switch
        {
            RunOutcome.Victory => "胜利！",
            RunOutcome.Defeat => "失败！",
            _ => "本局结算",
        };

        /// <summary>一句话结论；三种结局各自说清 day 指的是哪一天。</summary>
        public static string Summary(RunOutcome outcome, int day) => outcome switch
        {
            RunOutcome.Victory => $"撑过了第 {SurvivedDays(day)} 天",
            RunOutcome.Defeat => $"据点在第 {day} 天被摧毁",
            _ => $"在第 {day} 天主动收局",
        };

        public static Color ColorFor(RunOutcome outcome) => outcome switch
        {
            RunOutcome.Victory => new Color(0.4f, 0.9f, 0.4f),
            RunOutcome.Defeat => new Color(0.95f, 0.35f, 0.35f),
            _ => new Color(0.45f, 0.85f, 1f),
        };

        /// <summary>结算页的统计块（多行文本）。</summary>
        public static string Stats(RunSummary summary)
        {
            if (summary == null)
            {
                return string.Empty;
            }

            var phase = summary.IsNight ? "夜晚" : "白天";
            var buildings = SettlementService.DescribeBuildings(summary);
            var lines = new List<string>
            {
                $"第 {summary.Day} 天（{phase}）　据点 {summary.BaseHp}/{summary.BaseMaxHp}　行动点 {summary.ActionPoints}",
                $"建筑 {summary.TotalBuildings} 座：{buildings}",
                $"资源　木 {summary.Wood}　石 {summary.Stone}　食 {summary.Food}　建材 {summary.Material}　修理包 {summary.RepairKit}",
                $"仓库容量 {summary.Capacity}",
            };

            return string.Join("\n", lines);
        }
    }
}
