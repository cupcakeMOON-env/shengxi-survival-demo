using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class CollectorSystemTests
    {
        [Test]
        public void Tick_CollectsFromCollectorTile()
        {
            var map = new GridMap(5, 5);
            map.SetTerrain(new GridPos(2, 2), TerrainType.Forest, 5);
            map.Place(BuildingType.Collector, new GridPos(2, 2));
            var pool = new ResourcePool();

            CollectorSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(1));
            Assert.That(map.GetTile(new GridPos(2, 2)).ResourceAmount, Is.EqualTo(4));
        }

        [Test]
        public void Tick_DepletesTile_ThenStops()
        {
            var map = new GridMap(5, 5);
            map.SetTerrain(new GridPos(2, 2), TerrainType.Stone, 1);
            map.Place(BuildingType.Collector, new GridPos(2, 2));
            var pool = new ResourcePool();

            CollectorSystem.Tick(map, pool);
            CollectorSystem.Tick(map, pool);

            Assert.That(pool.GetAmount(ResourceType.Stone), Is.EqualTo(1));
            Assert.That(map.GetTile(new GridPos(2, 2)).Terrain, Is.EqualTo(TerrainType.Grass));
        }
    }
}
