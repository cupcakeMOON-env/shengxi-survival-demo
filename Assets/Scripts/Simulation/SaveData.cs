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
        public int version = 2;
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
