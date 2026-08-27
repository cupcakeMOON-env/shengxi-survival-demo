using System.Collections.Generic;
using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class SaveSerializerTests
    {
        [Test]
        public void RoundTrip_PreservesState()
        {
            var map = MapGenerator.CreateRandomMap(30, 30, 99);
            var pool = new ResourcePool { Capacity = 300 };
            pool.Add(ResourceType.Wood, 12);
            pool.Add(ResourceType.Stone, 7);
            pool.Add(ResourceType.Food, 3);
            var ap = new ActionPointSystem(10);
            ap.Spend(4);
            var cycle = new DayCycle();
            cycle.StartNight();
            var baseDefense = new Base(new GridPos(15, 15), 20);
            baseDefense.TakeDamage(5);
            var enemies = new List<Enemy> { new Enemy(1, new GridPos(10, 10), 3, 1) };

            var data = SaveSerializer.Build(map, pool, ap, cycle, baseDefense, enemies, 99);

            var map2 = SaveSerializer.RebuildMap(data);
            var pool2 = SaveSerializer.RebuildPool(data);
            var ap2 = SaveSerializer.RebuildActionPoints(data);
            var cycle2 = SaveSerializer.RebuildCycle(data);
            var base2 = SaveSerializer.RebuildBase(data);
            var enemies2 = SaveSerializer.RebuildEnemies(data);

            Assert.That(pool2.GetAmount(ResourceType.Wood), Is.EqualTo(12));
            Assert.That(pool2.GetAmount(ResourceType.Stone), Is.EqualTo(7));
            Assert.That(pool2.GetAmount(ResourceType.Food), Is.EqualTo(3));
            Assert.That(pool2.Capacity, Is.EqualTo(300));
            Assert.That(ap2.Current, Is.EqualTo(6));
            Assert.That(cycle2.IsNight, Is.True);
            Assert.That(cycle2.Day, Is.EqualTo(1));
            Assert.That(base2.CurrentHp, Is.EqualTo(15));
            Assert.That(enemies2.Count, Is.EqualTo(1));
            Assert.That(enemies2[0].Position, Is.EqualTo(new GridPos(10, 10)));
            Assert.That(map2.GetTile(new GridPos(3, 3)).Terrain, Is.EqualTo(map.GetTile(new GridPos(3, 3)).Terrain));
        }

        [Test]
        public void RoundTrip_PreservesBuildingsAndTiles()
        {
            var map = new GridMap(10, 10);
            map.SetTerrain(new GridPos(2, 2), TerrainType.Forest, 5);
            map.Place(BuildingType.Collector, new GridPos(2, 2));
            map.Place(BuildingType.ArrowTower, new GridPos(3, 3));
            map.SetBuildingHp(new GridPos(3, 3), 2); // 模拟箭塔被敌人打掉 3 血
            map.Place(BuildingType.Base, new GridPos(5, 5));

            var data = SaveSerializer.Build(
                map,
                new ResourcePool(),
                new ActionPointSystem(10),
                new DayCycle(),
                new Base(new GridPos(5, 5), 20),
                new List<Enemy>(),
                1);

            var restored = SaveSerializer.RebuildMap(data);

            Assert.That(restored.GetTile(new GridPos(2, 2)).Building, Is.EqualTo(BuildingType.Collector));
            Assert.That(restored.GetTile(new GridPos(3, 3)).Building, Is.EqualTo(BuildingType.ArrowTower));
            Assert.That(restored.GetTile(new GridPos(3, 3)).BuildingHp, Is.EqualTo(2), "建筑血量应随存档还原");
            Assert.That(restored.GetTile(new GridPos(5, 5)).Building, Is.EqualTo(BuildingType.Base));
            Assert.That(restored.GetTile(new GridPos(2, 2)).ResourceAmount, Is.EqualTo(5));
            Assert.That(restored.GetTile(new GridPos(2, 2)).Terrain, Is.EqualTo(TerrainType.Forest));
        }

        [Test]
        public void Migrator_UpgradesOldVersion()
        {
            var old = new SaveData { version = 0 };

            var upgraded = SaveMigrator.Upgrade(old);

            Assert.That(upgraded.version, Is.EqualTo(SaveMigrator.CurrentVersion));
            Assert.That(upgraded.food, Is.Zero);
            Assert.That(upgraded.capacity, Is.EqualTo(100));
            Assert.That(upgraded.tiles, Is.Not.Null);
            Assert.That(upgraded.enemies, Is.Not.Null);
        }

        [Test]
        public void Migrator_UpgradesV1Save_FillsBuildingHp()
        {
            var old = new SaveData
            {
                version = 1,
                tiles = new[]
                {
                    new SaveTileData { x = 1, y = 1, building = (int)BuildingType.Wall },
                    new SaveTileData { x = 2, y = 2, building = (int)BuildingType.None },
                },
            };

            var upgraded = SaveMigrator.Upgrade(old);

            Assert.That(upgraded.version, Is.EqualTo(SaveMigrator.CurrentVersion));
            Assert.That(
                upgraded.tiles[0].buildingHp,
                Is.EqualTo(BuildingCatalog.Get(BuildingType.Wall).MaxHp),
                "旧档建筑应按目录血量补满");
            Assert.That(upgraded.tiles[1].buildingHp, Is.Zero, "空地血量保持 0");
        }

        [Test]
        public void Migrator_HandlesNull()
        {
            Assert.That(SaveMigrator.Upgrade(null), Is.Null);
        }
    }
}
