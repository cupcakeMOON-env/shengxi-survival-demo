using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class GridMapTests
    {
        [Test]
        public void Constructor_CreatesGrassMapOfRequestedSize()
        {
            var map = new GridMap(10, 8);

            Assert.That(map.Width, Is.EqualTo(10));
            Assert.That(map.Height, Is.EqualTo(8));
            Assert.That(map.GetTile(new GridPos(0, 0)).Terrain, Is.EqualTo(TerrainType.Grass));
            Assert.That(map.GetTile(new GridPos(9, 7)).Terrain, Is.EqualTo(TerrainType.Grass));
        }

        [Test]
        public void IsInside_RejectsOutOfBounds()
        {
            var map = new GridMap(5, 5);

            Assert.That(map.IsInside(new GridPos(0, 0)), Is.True);
            Assert.That(map.IsInside(new GridPos(-1, 0)), Is.False);
            Assert.That(map.IsInside(new GridPos(5, 4)), Is.False);
            Assert.That(map.IsInside(new GridPos(0, 5)), Is.False);
        }

        [Test]
        public void GetTile_OutOfBounds_ReturnsNull()
        {
            var map = new GridMap(5, 5);

            Assert.That(map.GetTile(new GridPos(-1, -1)), Is.Null);
        }

        [Test]
        public void SetTerrain_SetsTerrainAndResource()
        {
            var map = new GridMap(5, 5);
            var pos = new GridPos(2, 3);

            map.SetTerrain(pos, TerrainType.Forest, 42);

            Assert.That(map.GetTile(pos).Terrain, Is.EqualTo(TerrainType.Forest));
            Assert.That(map.GetTile(pos).ResourceAmount, Is.EqualTo(42));
        }

        [Test]
        public void Place_OnEmptyGrass_SucceedsAndOccupies()
        {
            var map = new GridMap(5, 5);
            var pos = new GridPos(1, 1);

            Assert.That(map.CanPlace(BuildingType.ArrowTower, pos), Is.True);
            Assert.That(map.Place(BuildingType.ArrowTower, pos), Is.True);
            Assert.That(map.GetTile(pos).Building, Is.EqualTo(BuildingType.ArrowTower));
            Assert.That(map.CanPlace(BuildingType.Wall, pos), Is.False);
        }

        [Test]
        public void Place_OnWallTile_IsBlocked()
        {
            var map = new GridMap(5, 5);
            var pos = new GridPos(2, 2);

            map.Place(BuildingType.Wall, pos);

            Assert.That(map.GetTile(pos).IsWalkable, Is.False);
            Assert.That(map.CanPlace(BuildingType.ArrowTower, pos), Is.False);
        }

        [Test]
        public void RemoveBuilding_FreesTile()
        {
            var map = new GridMap(5, 5);
            var pos = new GridPos(3, 3);

            map.Place(BuildingType.Workshop, pos);
            map.RemoveBuilding(pos);

            Assert.That(map.GetTile(pos).Building, Is.EqualTo(BuildingType.None));
            Assert.That(map.CanPlace(BuildingType.Workshop, pos), Is.True);
        }

        [Test]
        public void DemoMap_HasWaterForestAndStone()
        {
            var map = MapGenerator.CreateDemoMap();

            Assert.That(map.GetTile(new GridPos(6, 6)).Terrain, Is.EqualTo(TerrainType.Water));
            Assert.That(map.GetTile(new GridPos(6, 23)).Terrain, Is.EqualTo(TerrainType.Forest));
            Assert.That(map.GetTile(new GridPos(25, 18)).Terrain, Is.EqualTo(TerrainType.Stone));
            Assert.That(map.GetTile(new GridPos(25, 18)).ResourceAmount, Is.GreaterThan(0));
        }
    }
}
