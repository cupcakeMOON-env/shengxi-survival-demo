using System;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 存档数据（纯 C# DTO）：只存 ID / 坐标 / 数值状态，不存任何 GameObject 引用。
    /// 既可以被 JsonUtility 序列化到文件，也可以在模拟层单独测试。
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 4;
        public int seed;
        public int width;
        public int height;
        public int day;
        public bool isNight;
        public int actionPoints;
        public int capacity;
        public int wood;
        public int stone;
        public int food;
        public int material; // 建材：工坊产物，v3 加入
        public int repairKit; // 修理包：工坊产物，v4 加入
        public int baseX;
        public int baseY;
        public int baseHp;
        public int baseMaxHp;
        public SaveTileData[] tiles;
        public SaveEnemyData[] enemies;
    }

    [Serializable]
    public class SaveTileData
    {
        public int x;
        public int y;
        public int terrain;
        public int resourceAmount;
        public int building;
        public int buildingHp;
        public int level; // 建筑等级：v3 加入（无建筑为 0）
    }

    [Serializable]
    public class SaveEnemyData
    {
        public int id;
        public int x;
        public int y;
        public int hp;
        public int maxHp;
        public int damage;
    }
}
