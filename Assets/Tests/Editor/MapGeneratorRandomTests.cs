using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class MapGeneratorRandomTests
    {
        private static int TerrainCount(GridMap map, TerrainType type)
        {
            var count = 0;
            for (var x = 0; x < map.Width; x++)
            {
                for (var y = 0; y < map.Height; y++)
                {
                    if (map.GetTile(new GridPos(x, y)).Terrain == type)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        [Test]
        public void SameSeed_ProducesSameMap()
        {
            var a = MapGenerator.CreateRandomMap(30, 30, 12345);
            var b = MapGenerator.CreateRandomMap(30, 30, 12345);

            Assert.That(TerrainCount(a, TerrainType.Forest), Is.EqualTo(TerrainCount(b, TerrainType.Forest)));
            Assert.That(TerrainCount(a, TerrainType.Water), Is.EqualTo(TerrainCount(b, TerrainType.Water)));
            Assert.That(a.GetTile(new GridPos(3, 3)).ResourceAmount, Is.EqualTo(b.GetTile(new GridPos(3, 3)).ResourceAmount));
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentMaps()
        {
            var a = MapGenerator.CreateRandomMap(30, 30, 12345);
            var b = MapGenerator.CreateRandomMap(30, 30, 54321);

            var differs = false;
            for (var x = 0; x < 30 && !differs; x++)
            {
                for (var y = 0; y < 30; y++)
                {
                    if (a.GetTile(new GridPos(x, y)).Terrain != b.GetTile(new GridPos(x, y)).Terrain)
                    {
                        differs = true;
                        break;
                    }
                }
            }

            Assert.That(differs, Is.True, "不同 seed 应生成不同地图");
        }

        [Test]
        public void Center_IsWalkableGrass()
        {
            var map = MapGenerator.CreateRandomMap(30, 30, 7);
            var center = new GridPos(15, 15);

            Assert.That(map.GetTile(center).Terrain, Is.EqualTo(TerrainType.Grass));
            Assert.That(map.GetTile(center).IsWalkable, Is.True);
        }

        [Test]
        public void RandomMap_HasResources()
        {
            var map = MapGenerator.CreateRandomMap(30, 30, 42);

            Assert.That(TerrainCount(map, TerrainType.Forest), Is.GreaterThan(0));
            Assert.That(TerrainCount(map, TerrainType.Stone), Is.GreaterThan(0));
            Assert.That(TerrainCount(map, TerrainType.Bush), Is.GreaterThan(0));
        }
    }
}
