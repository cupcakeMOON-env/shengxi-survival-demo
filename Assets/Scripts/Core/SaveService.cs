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
            File.WriteAllText(Path.Combine(Application.persistentDataPath, fileName), JsonUtility.ToJson(data, true));
        }

        public static SaveData Read(string fileName = "shengxi_save.json")
        {
            var path = Path.Combine(Application.persistentDataPath, fileName);
            if (!File.Exists(path))
            {
                return null;
            }

            return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }
    }
}
