using System;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 存档版本迁移：把旧版本存档升级到当前版本。
    /// 约定：任何字段变更都必须递增版本号并在这里补迁移逻辑，禁止原地改旧数据。
    /// </summary>
    public static class SaveMigrator
    {
        public const int CurrentVersion = 4;

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

            if (data.version < 2)
            {
                // v1 → v2：建筑新增血量字段；旧档建筑按目录血量补齐（视为满血）
                if (data.tiles != null)
                {
                    foreach (var t in data.tiles)
                    {
                        if (t.building != (int)BuildingType.None)
                        {
                            var def = BuildingCatalog.Get((BuildingType)t.building);
                            t.buildingHp = def != null && def.MaxHp > 0 ? def.MaxHp : 1;
                        }
                    }
                }

                data.version = 2;
            }

            if (data.version < 3)
            {
                // v2 → v3：建筑新增等级字段、资源池新增建材。
                // 旧档建筑视为 1 级（血量已存、等级换算后最大血量等于目录血量，语义一致）；
                // 建材数量旧档没有，保持 0。
                if (data.tiles != null)
                {
                    foreach (var t in data.tiles)
                    {
                        t.level = t.building != (int)BuildingType.None ? 1 : 0;
                    }
                }

                data.version = 3;
            }

            if (data.version < 4)
            {
                // v3 → v4：资源池新增修理包。旧档没有该字段，JsonUtility 反序列化后即为 0，
                // 这里只做版本推进（语义等价于修理包 0）。
                data.version = 4;
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
