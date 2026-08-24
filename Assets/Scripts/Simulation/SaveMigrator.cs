using System;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 存档版本迁移：把旧版本存档升级到当前版本。
    /// 约定：任何字段变更都必须递增版本号并在这里补迁移逻辑，禁止原地改旧数据。
    /// </summary>
    public static class SaveMigrator
    {
        public const int CurrentVersion = 1;

        public static SaveData Upgrade(SaveData data)
        {
            if (data == null)
            {
                return null;
            }

            if (data.version < 1)
            {
                // v0（早期原型，没有食物/容量字段）→ v1
                data.food = 0;
                data.capacity = data.capacity > 0 ? data.capacity : 100;
                data.version = 1;
            }

            if (data.tiles == null)
            {
                data.tiles = Array.Empty<SaveTileData>();
            }

            if (data.enemies == null)
            {
                data.enemies = Array.Empty<SaveEnemyData>();
            }

            return data;
        }
    }
}
