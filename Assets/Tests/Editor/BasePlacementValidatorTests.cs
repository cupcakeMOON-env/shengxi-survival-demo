using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class BasePlacementValidatorTests
    {
        [Test]
        public void CanPlace_OnEmptyGrass_True()
        {
            var map = new GridMap(10, 10);

            Assert.That(BasePlacementValidator.CanPlace(map, new GridPos(3, 3)), Is.True);
        }

        [Test]
        public void CanPlace_OnWater_False()
        {
            var map = new GridMap(10, 10);
            map.SetTerrain(new GridPos(2, 2), TerrainType.Water, 0);

            Assert.That(BasePlacementValidator.CanPlace(map, new GridPos(2, 2)), Is.False);
        }

        [Test]
        public void CanPlace_OnOccupiedTile_False()
        {
            var map = new GridMap(10, 10);
            map.Place(BuildingType.ArrowTower, new GridPos(4, 4));

            Assert.That(BasePlacementValidator.CanPlace(map, new GridPos(4, 4)), Is.False);
        }

        [Test]
        public void CanPlace_OutOfBounds_False()
        {
            var map = new GridMap(10, 10);

            Assert.That(BasePlacementValidator.CanPlace(map, new GridPos(-1, -1)), Is.False);
            Assert.That(BasePlacementValidator.CanPlace(map, new GridPos(10, 10)), Is.False);
        }

        [Test]
        public void CanPlace_InWaterIsolatedPocket_False()
        {
            var map = new GridMap(10, 10);
            // 用一圈水把 (5,5) 隔离成独立小岛，敌人永远到不了
            map.SetTerrain(new GridPos(5, 4), TerrainType.Water, 0);
            map.SetTerrain(new GridPos(5, 6), TerrainType.Water, 0);
            map.SetTerrain(new GridPos(4, 5), TerrainType.Water, 0);
            map.SetTerrain(new GridPos(6, 5), TerrainType.Water, 0);

            Assert.That(BasePlacementValidator.CanPlace(map, new GridPos(5, 5)), Is.False, "孤立水盆不可放据点");
            Assert.That(BasePlacementValidator.CanPlace(map, new GridPos(3, 3)), Is.True, "连通大陆可以放据点");
        }
    }
}
