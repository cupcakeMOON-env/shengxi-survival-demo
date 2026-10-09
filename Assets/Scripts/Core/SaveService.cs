using System;
using System.Collections.Generic;
using System.IO;
using ShengXi.Simulation;
using UnityEngine;

namespace ShengXi.Core
{
    /// <summary>
    /// 存档文件读写：JsonUtility 序列化 SaveData 到 persistentDataPath。
    /// 只做 IO，状态打包/还原都在 Simulation 的 SaveSerializer 里（可单测）。
    /// </summary>
    public static class SaveService
    {
        public static void Write(SaveData data, string fileName = "shengxi_save.json")
        {
            File.WriteAllText(PathOf(fileName), JsonUtility.ToJson(data, true));
        }

        public static SaveData Read(string fileName = "shengxi_save.json")
        {
            var path = PathOf(fileName);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                // 存档被外部改坏时不该把游戏带崩，当作"这个槽位没有可用存档"
                Debug.LogWarning($"[Save] 读取 {fileName} 失败：{e.Message}");
                return null;
            }
        }

        public static string PathOf(string fileName) => Path.Combine(Application.persistentDataPath, fileName);

        public static bool Exists(string fileName) => File.Exists(PathOf(fileName));

        /// <summary>删除某个槽位的存档文件；文件本来就不存在时也返回 true。</summary>
        public static bool Delete(string fileName)
        {
            try
            {
                var path = PathOf(fileName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] 删除 {fileName} 失败：{e.Message}");
                return false;
            }
        }

        /// <summary>读取全部固定槽位的概况，供开始界面 / 存档面板列出。</summary>
        public static List<SaveSlotInfo> InspectAllSlots()
        {
            var infos = new List<SaveSlotInfo>();
            var slots = SaveSlots.All;
            for (var i = 0; i < slots.Count; i++)
            {
                infos.Add(Inspect(slots[i]));
            }

            return infos;
        }

        /// <summary>读取单个槽位的概况：有没有存档、第几天、白天夜晚、文件写入时间。</summary>
        public static SaveSlotInfo Inspect(string slotId)
        {
            var info = new SaveSlotInfo
            {
                SlotId = slotId,
                DisplayName = SaveSlots.DisplayName(slotId),
            };

            if (!SaveSlots.IsValid(slotId))
            {
                return info;
            }

            var path = PathOf(slotId);
            info.Exists = File.Exists(path);
            if (!info.Exists)
            {
                return info;
            }

            try
            {
                info.SavedAt = File.GetLastWriteTime(path);
            }
            catch (Exception)
            {
                info.SavedAt = null;
            }

            var data = Read(slotId);
            if (data == null)
            {
                info.Exists = false;
                return info;
            }

            info.Day = data.day;
            info.IsNight = data.isNight;
            info.Version = data.version;
            return info;
        }
    }

    /// <summary>槽位概况（开始界面显示"存档 2　第 3 天 · 白天　07-20 14:32"用）。</summary>
    public class SaveSlotInfo
    {
        public string SlotId;
        public string DisplayName;
        public bool Exists;
        public int Day;
        public bool IsNight;
        public int Version;
        public DateTime? SavedAt;

        /// <summary>一句话描述，空槽位显示"空"。</summary>
        public string Describe()
        {
            if (!Exists)
            {
                return $"{DisplayName}　空";
            }

            var phase = IsNight ? "夜晚" : "白天";
            var time = SavedAt.HasValue ? SavedAt.Value.ToString("MM-dd HH:mm") : "--";
            return $"{DisplayName}　第 {Day} 天 · {phase}　{time}";
        }
    }
}
