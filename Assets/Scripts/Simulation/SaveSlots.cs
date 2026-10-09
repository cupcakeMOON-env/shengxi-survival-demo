using System.Collections.Generic;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 存档位定义：固定三个位置。
    /// 槽位 Id 同时就是文件名，UI、存档 IO、测试都读这一份定义，
    /// 避免"界面上写着存档 2、实际写到别的文件"这类对不上的问题。
    /// 纯 C#，可单测。
    /// </summary>
    public static class SaveSlots
    {
        public const int Count = 3;

        private static readonly string[] SlotIds =
        {
            "shengxi_slot1.json",
            "shengxi_slot2.json",
            "shengxi_slot3.json",
        };

        /// <summary>全部槽位 Id（顺序即 UI 顺序）。</summary>
        public static IReadOnlyList<string> All => SlotIds;

        /// <summary>按下标取槽位 Id；越界返回 null。</summary>
        public static string IdAt(int index) =>
            index >= 0 && index < SlotIds.Length ? SlotIds[index] : null;

        /// <summary>下标；不是合法槽位时返回 -1。</summary>
        public static int IndexOf(string slotId)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                return -1;
            }

            for (var i = 0; i < SlotIds.Length; i++)
            {
                if (SlotIds[i] == slotId)
                {
                    return i;
                }
            }

            return -1;
        }

        public static bool IsValid(string slotId) => IndexOf(slotId) >= 0;

        /// <summary>给玩家看的名字，例如「存档 2」。</summary>
        public static string DisplayName(string slotId)
        {
            var index = IndexOf(slotId);
            return index >= 0 ? $"存档 {index + 1}" : "未知存档位";
        }
    }
}
