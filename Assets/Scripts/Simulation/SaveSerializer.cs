using System.Collections.Generic;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 存档序列化：纯 C#，负责「游戏状态 → SaveData」和「SaveData → 重建各系统」。
    /// 与文件读写（SaveService）分离，保证模拟层可脱离 Unity 测试。
    /// </summary>
    public static class SaveSerializer
    {
        public static SaveData Build(
            GridMap map,
            ResourcePool pool,
            ActionPointSystem ap,
            DayCycle cycle,
            Base baseDefense,
            List<Enemy> enemies,
            int seed)
        {
            var data = new SaveData
            {
                version = SaveMigrator.CurrentVersion,
                seed = seed,
                width = map.Width,
                height = map.Height,
                day = cycle.Day,
                isNight = cycle.IsNight,
                actionPoints = ap.Current,
                capacity = pool.Capacity,
                wood = pool.GetAmount(ResourceType.Wood),
                stone = pool.GetAmount(ResourceType.Stone),
                food = pool.GetAmount(ResourceType.Food),
                baseX = baseDefense.Position.X,
                baseY = baseDefense.Position.Y,
                baseHp = baseDefense.CurrentHp,
                baseMaxHp = baseDefense.MaxHp,
            };

            var tileList = new List<SaveTileData>();
            for (var x = 0; x < map.Width; x++)
            {
                for (var y = 0; y < map.Height; y++)
                {
                    var tile = map.GetTile(new GridPos(x, y));
                    tileList.Add(new SaveTileData
                    {
                        x = x,
                        y = y,
                        terrain = (int)tile.Terrain,
                        resourceAmount = tile.ResourceAmount,
                        building = (int)tile.Building,
                        buildingHp = tile.BuildingHp,
                    });
                }
            }

            data.tiles = tileList.ToArray();

            var enemyList = new List<SaveEnemyData>();
            foreach (var enemy in enemies)
            {
                enemyList.Add(new SaveEnemyData
                {
                    id = enemy.Id,
                    x = enemy.Position.X,
                    y = enemy.Position.Y,
                    hp = enemy.HP,
                    maxHp = enemy.MaxHp,
                    damage = enemy.Damage,
                });
            }

            data.enemies = enemyList.ToArray();
            return data;
        }

        public static GridMap RebuildMap(SaveData data)
        {
            var map = new GridMap(data.width, data.height);
            if (data.tiles == null)
            {
                return map;
            }

            foreach (var t in data.tiles)
            {
                map.SetTerrain(new GridPos(t.x, t.y), (TerrainType)t.terrain, t.resourceAmount);
            }

            foreach (var t in data.tiles)
            {
                var building = (BuildingType)t.building;
                if (building == BuildingType.None)
                {
                    continue;
                }

                map.Place(building, new GridPos(t.x, t.y));
                map.SetBuildingHp(new GridPos(t.x, t.y), t.buildingHp);
            }

            return map;
        }

        public static ResourcePool RebuildPool(SaveData data)
        {
            var pool = new ResourcePool();
            pool.Restore(data.wood, data.stone, data.food, data.capacity);
            return pool;
        }

        public static ActionPointSystem RebuildActionPoints(SaveData data)
        {
            var ap = new ActionPointSystem(10);
            ap.Restore(data.actionPoints);
            return ap;
        }

        public static DayCycle RebuildCycle(SaveData data)
        {
            var cycle = new DayCycle();
            cycle.Restore(data.day, data.isNight);
            return cycle;
        }

        public static Base RebuildBase(SaveData data)
        {
            var baseDefense = new Base(new GridPos(data.baseX, data.baseY), data.baseMaxHp);
            baseDefense.Restore(data.baseHp);
            return baseDefense;
        }

        public static List<Enemy> RebuildEnemies(SaveData data)
        {
            var enemies = new List<Enemy>();
            if (data.enemies == null)
            {
                return enemies;
            }

            foreach (var e in data.enemies)
            {
                var enemy = new Enemy(e.id, new GridPos(e.x, e.y), e.maxHp, e.damage);
                enemy.HP = e.hp;
                enemies.Add(enemy);
            }

            return enemies;
        }
    }
}
