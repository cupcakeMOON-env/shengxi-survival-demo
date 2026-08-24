using System.Linq;
using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class BuildingCatalogTests
    {
        [Test]
        public void AllBuildingTypesAreUnique()
        {
            var types = BuildingCatalog.All.Select(d => d.Type).ToList();

            Assert.That(types.Distinct().Count(), Is.EqualTo(types.Count));
        }

        [Test]
        public void EveryDef_HasNameAndValidCosts()
        {
            foreach (var def in BuildingCatalog.All)
            {
                Assert.That(string.IsNullOrEmpty(def.Name), Is.False, $"{def.Type} name");
                Assert.That(def.WoodCost, Is.GreaterThanOrEqualTo(0), $"{def.Type} wood");
                Assert.That(def.StoneCost, Is.GreaterThanOrEqualTo(0), $"{def.Type} stone");
                Assert.That(def.FoodCost, Is.GreaterThanOrEqualTo(0), $"{def.Type} food");
                Assert.That(def.ActionPointCost, Is.GreaterThan(0), $"{def.Type} AP");
            }
        }

        [Test]
        public void Get_ReturnsDefForType()
        {
            Assert.That(BuildingCatalog.Get(BuildingType.Warehouse).Name, Is.EqualTo("仓库"));
            Assert.That(BuildingCatalog.Get(BuildingType.Collector).RequiresResourceTile, Is.True);
            Assert.That(BuildingCatalog.Get(BuildingType.ArrowTower), Is.Not.Null);
        }
    }
}
