using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class BuildServiceTests
    {
        private static (GridMap map, ResourcePool pool, ActionPointSystem ap) Setup()
        {
            var map = new GridMap(10, 10);
            map.SetTerrain(new GridPos(2, 2), TerrainType.Forest, 10);
            return (map, new ResourcePool(), new ActionPointSystem(10));
        }

        [Test]
        public void Build_WithoutResources_Fails()
        {
            var (map, pool, ap) = Setup();
            var def = BuildingCatalog.Get(BuildingType.Warehouse);

            var ok = BuildService.TryBuild(map, pool, ap, def, new GridPos(0, 0));

            Assert.That(ok, Is.False);
            Assert.That(map.GetTile(new GridPos(0, 0)).Building, Is.EqualTo(BuildingType.None));
        }

        [Test]
        public void Build_WithResourcesAndAp_SucceedsAndDeducts()
        {
            var (map, pool, ap) = Setup();
            pool.Add(ResourceType.Wood, 10);
            pool.Add(ResourceType.Stone, 5);

            var ok = BuildService.TryBuild(map, pool, ap, BuildingCatalog.Get(BuildingType.Warehouse), new GridPos(0, 0));

            Assert.That(ok, Is.True);
            Assert.That(map.GetTile(new GridPos(0, 0)).Building, Is.EqualTo(BuildingType.Warehouse));
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.Zero);
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.Zero);
            Assert.That(ap.Current, Is.EqualTo(8));
        }

        [Test]
        public void Build_OnOccupiedTile_Fails()
        {
            var (map, pool, ap) = Setup();
            pool.Add(ResourceType.Wood, 30);
            pool.Add(ResourceType.Stone, 20);
            var def = BuildingCatalog.Get(BuildingType.Warehouse);
            map.Place(BuildingType.Wall, new GridPos(1, 1));

            var ok = BuildService.TryBuild(map, pool, ap, def, new GridPos(1, 1));

            Assert.That(ok, Is.False);
        }

        [Test]
        public void Collector_RequiresResourceTile()
        {
            var (map, pool, ap) = Setup();
            pool.Add(ResourceType.Wood, 20);
            pool.Add(ResourceType.Stone, 10);
            var def = BuildingCatalog.Get(BuildingType.Collector);

            Assert.That(BuildService.TryBuild(map, pool, ap, def, new GridPos(5, 5)), Is.False, "草地不能建采集站");
            Assert.That(BuildService.TryBuild(map, pool, ap, def, new GridPos(2, 2)), Is.True, "森林格可以建采集站");
        }

        [Test]
        public void Warehouse_IncreasesCapacity()
        {
            var (map, pool, ap) = Setup();
            pool.Add(ResourceType.Wood, 10);
            pool.Add(ResourceType.Stone, 5);

            BuildService.TryBuild(map, pool, ap, BuildingCatalog.Get(BuildingType.Warehouse), new GridPos(0, 0));

            Assert.That(pool.Capacity, Is.EqualTo(200));
        }

        [Test]
        public void Wall_MakesTileUnwalkable()
        {
            var (map, pool, ap) = Setup();
            pool.Add(ResourceType.Wood, 5);

            BuildService.TryBuild(map, pool, ap, BuildingCatalog.Get(BuildingType.Wall), new GridPos(3, 3));

            Assert.That(map.GetTile(new GridPos(3, 3)).IsWalkable, Is.False);
        }

        [Test]
        public void Build_InsufficientActionPoints_Fails()
        {
            var (map, pool, ap) = Setup();
            pool.Add(ResourceType.Wood, 30);
            pool.Add(ResourceType.Stone, 20);
            ap.Spend(9);

            var ok = BuildService.TryBuild(map, pool, ap, BuildingCatalog.Get(BuildingType.ArrowTower), new GridPos(0, 0));

            Assert.That(ok, Is.False);
        }
    }
}
