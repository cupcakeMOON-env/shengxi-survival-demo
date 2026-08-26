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
    }
}
