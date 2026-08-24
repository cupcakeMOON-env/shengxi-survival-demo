using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class CollectServiceTests
    {
        private static GridMap MapWith(TerrainType terrain, int amount)
        {
            var map = new GridMap(5, 5);
            map.SetTerrain(new GridPos(2, 2), terrain, amount);
            return map;
        }

        [Test]
        public void CollectFromForest_GivesWoodAndReducesTile()
        {
            var map = MapWith(TerrainType.Forest, 5);
            var pool = new ResourcePool();

            var ok = CollectService.TryCollect(map, pool, new GridPos(2, 2));

            Assert.That(ok, Is.True);
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(CollectService.YieldPerCollect));
            Assert.That(map.GetTile(new GridPos(2, 2)).ResourceAmount, Is.EqualTo(5 - CollectService.YieldPerCollect));
        }

        [Test]
        public void CollectFromStone_GivesStone()
        {
            var map = MapWith(TerrainType.Stone, 3);
            var pool = new ResourcePool();

            var ok = CollectService.TryCollect(map, pool, new GridPos(2, 2));

            Assert.That(ok, Is.True);
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.EqualTo(CollectService.YieldPerCollect));
        }

        [Test]
        public void CollectFromBush_GivesFood()
        {
            var map = MapWith(TerrainType.Bush, 3);
            var pool = new ResourcePool();

            var ok = CollectService.TryCollect(map, pool, new GridPos(2, 2));

            Assert.That(ok, Is.True);
            Assert.That(pool.GetAmount(ResourceType.Food), Is.EqualTo(CollectService.YieldPerCollect));
        }

        [Test]
        public void CollectFromGrass_Fails()
        {
            var map = MapWith(TerrainType.Grass, 0);
            var pool = new ResourcePool();

            var ok = CollectService.TryCollect(map, pool, new GridPos(2, 2));

            Assert.That(ok, Is.False);
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.Zero);
        }

        [Test]
        public void CollectOutOfBounds_Fails()
        {
            var map = new GridMap(5, 5);
            var pool = new ResourcePool();

            var ok = CollectService.TryCollect(map, pool, new GridPos(-1, 0));

            Assert.That(ok, Is.False);
        }

        [Test]
        public void DepletedTile_TurnsToGrassAndCannotBeCollectedAgain()
        {
            var map = MapWith(TerrainType.Forest, 1);
            var pool = new ResourcePool();

            Assert.That(CollectService.TryCollect(map, pool, new GridPos(2, 2)), Is.True);
            Assert.That(map.GetTile(new GridPos(2, 2)).Terrain, Is.EqualTo(TerrainType.Grass));
            Assert.That(CollectService.TryCollect(map, pool, new GridPos(2, 2)), Is.False);
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(1));
        }
    }
}
