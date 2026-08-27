using System.Collections;
using System.IO;
using NUnit.Framework;
using ShengXi.Core;
using ShengXi.Simulation;
using ShengXi.UI;
using ShengXi.View;
using UnityEngine;
using UnityEngine.TestTools;

namespace ShengXi.Tests.PlayMode
{
    /// <summary>
    /// PlayMode 冒烟测试：真正进入运行时，验证 GameBootstrap 装配出的世界是完整的，
    /// 日夜循环能跑通，存档/读档能走通。
    /// 覆盖 EditMode 测不到的「装配层」（对象挂载、父子关系、事件接线）。
    /// </summary>
    public class SmokeTests
    {
        [UnityTest]
        public IEnumerator RuntimeWorld_BootstrapsCorrectly()
        {
            yield return null;
            yield return null;
            GameLoop.Instance.NewGame();
            yield return null;

            Assert.That(GameLoop.Instance, Is.Not.Null, "GameBootstrap 应创建 GameLoop");
            var loop = GameLoop.Instance;
            Assert.That(loop.Map, Is.Not.Null);
            Assert.That(loop.Map.Width, Is.EqualTo(30));
            Assert.That(loop.Map.Height, Is.EqualTo(30));
            Assert.That(loop.Cycle.IsDay, Is.True);
            Assert.That(loop.IsChoosingBase, Is.True, "开局应处于选择据点阶段");
            Assert.That(loop.Pool.GetAmount(ResourceType.Wood), Is.EqualTo(20), "开局应有初始木头（采集改为采集站产出）");
            Assert.That(loop.Pool.GetAmount(ResourceType.Stone), Is.EqualTo(15), "开局应有初始石头");

            var baseSpot = FindBuildableTile(loop.Map);
            Assert.That(baseSpot.HasValue, Is.True, "应存在可放置据点的格子");
            Assert.That(loop.ConfirmBase(baseSpot.Value), Is.True, "确认据点应成功");
            Assert.That(loop.IsChoosingBase, Is.False);
            Assert.That(loop.Base, Is.Not.Null);
            Assert.That(loop.Base.CurrentHp, Is.GreaterThan(0));
            Assert.That(loop.Map.GetTile(baseSpot.Value).Building, Is.EqualTo(BuildingType.Base), "据点应落到地图上");

            var cameras = Object.FindObjectsByType<Camera>();
            Assert.That(cameras.Length, Is.EqualTo(1), "应只有一个相机");
            Assert.That(cameras[0].orthographic, Is.True, "相机应为正交投影");

            var canvas = Object.FindAnyObjectByType<Canvas>();
            Assert.That(canvas, Is.Not.Null, "应有 Canvas");

            var gridView = Object.FindAnyObjectByType<GridView>();
            Assert.That(gridView, Is.Not.Null);
            Assert.That(gridView.transform.IsChildOf(canvas.transform), Is.False, "网格不应在 Canvas 下（历史 bug 回归）");

            var gridContainer = gridView.transform.Find("Grid");
            Assert.That(gridContainer, Is.Not.Null, "格子应统一挂在 Grid 容器下");
            Assert.That(
                gridContainer.childCount,
                Is.EqualTo(30 * 30),
                $"实际格子数 {gridContainer.childCount}");

            Assert.That(Object.FindAnyObjectByType<TileClickInput>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<BuildModeController>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<ResourceBar>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<BuildMenu>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<DayBanner>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<EnemyView>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<GameOverPanel>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator DayNightLoop_CompletesFirstNight()
        {
            yield return null;
            GameLoop.Instance.NewGame();
            yield return null;
            var loop = GameLoop.Instance;
            Assert.That(loop.Cycle.IsDay, Is.True);
            Assert.That(ConfirmBaseAtWalkable(loop), Is.True, "应能放置据点");

            loop.EndDay();
            Assert.That(loop.Cycle.IsNight, Is.True, "结束白天后应进入夜晚");
            Assert.That(loop.Enemies.Count, Is.GreaterThan(0), "夜晚应刷出敌人");

            var deadline = Time.realtimeSinceStartup + 120f;
            while (loop.Enemies.Count > 0 && !loop.GameOver && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(loop.Enemies.Count, Is.Zero, "敌人应被清空");
            Assert.That(loop.GameOver, Is.False, "第一晚不应失败");
            Assert.That(loop.Cycle.IsDay, Is.True, "夜晚结束应回到白天");
            Assert.That(loop.Cycle.Day, Is.EqualTo(2));
            Assert.That(loop.ActionPoints.Current, Is.EqualTo(loop.ActionPoints.MaxPerDay), "行动点应重置");
        }

        [UnityTest]
        public IEnumerator SaveLoad_RoundTrip_AtRuntime()
        {
            yield return null;
            GameLoop.Instance.NewGame();
            yield return null;
            const string slot = "smoke_test.json";
            var path = Path.Combine(Application.persistentDataPath, slot);

            try
            {
                var loop = GameLoop.Instance;
                Assert.That(loop.Cycle.IsDay, Is.True, "存档测试应从白天开始");
                Assert.That(ConfirmBaseAtWalkable(loop), Is.True, "应能放置据点");

                var forest = FindResourceTile(loop.Map, TerrainType.Forest);
                var stone = FindResourceTile(loop.Map, TerrainType.Stone);
                var buildSpot = FindBuildableTile(loop.Map);
                Assert.That(forest.HasValue && stone.HasValue && buildSpot.HasValue, Is.True, "地图上应有资源点和可建造位置");

                for (var i = 0; i < 12; i++)
                {
                    CollectService.TryCollect(loop.Map, loop.Pool, forest.Value);
                }

                for (var i = 0; i < 8; i++)
                {
                    CollectService.TryCollect(loop.Map, loop.Pool, stone.Value);
                }

                Assert.That(
                    BuildService.TryBuild(loop.Map, loop.Pool, loop.ActionPoints,
                        BuildingCatalog.Get(BuildingType.Warehouse), buildSpot.Value),
                    Is.True,
                    "应能成功建造仓库");

                var warehouseTile = FindTileRenderer(buildSpot.Value);
                Assert.That(warehouseTile, Is.Not.Null, "应能找到仓库所在格子的渲染物体");
                Assert.That(
                    warehouseTile.color,
                    Is.EqualTo(new Color(0.62f, 0.45f, 0.28f)),
                    "仓库格子应显示专属棕色（建筑颜色区分）");

                var woodSaved = loop.Pool.GetAmount(ResourceType.Wood);
                var capacitySaved = loop.Pool.Capacity;
                var apSaved = loop.ActionPoints.Current;
                var daySaved = loop.Cycle.Day;

                loop.SaveGame(slot);
                Assert.That(File.Exists(path), Is.True, "存档文件应生成");

                loop.Pool.TrySpend(ResourceType.Wood, 1);
                loop.LoadGame(slot);

                Assert.That(loop.Pool.GetAmount(ResourceType.Wood), Is.EqualTo(woodSaved), "木头应恢复");
                Assert.That(loop.Pool.Capacity, Is.EqualTo(capacitySaved), "容量应恢复");
                Assert.That(loop.ActionPoints.Current, Is.EqualTo(apSaved), "行动点应恢复");
                Assert.That(loop.Cycle.Day, Is.EqualTo(daySaved), "天数应恢复");
                Assert.That(loop.Map.GetTile(buildSpot.Value).Building, Is.EqualTo(BuildingType.Warehouse), "建筑应恢复");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [UnityTest]
        public IEnumerator Demolish_Runtime_FreesTileAndRefreshesView()
        {
            yield return null;
            GameLoop.Instance.NewGame();
            yield return null;
            var loop = GameLoop.Instance;
            Assert.That(ConfirmBaseAtWalkable(loop), Is.True, "应能放置据点");

            var buildSpot = FindBuildableTile(loop.Map);
            Assert.That(buildSpot.HasValue, Is.True, "应有可建造位置");
            var terrainRenderer = FindTileRenderer(buildSpot.Value);
            Assert.That(terrainRenderer, Is.Not.Null, "建造前应能找到格子渲染物体");
            var terrainColorBefore = terrainRenderer.color;

            Assert.That(
                BuildService.TryBuild(loop.Map, loop.Pool, loop.ActionPoints,
                    BuildingCatalog.Get(BuildingType.Warehouse), buildSpot.Value),
                Is.True,
                "应能成功建造仓库");
            Assert.That(loop.Pool.Capacity, Is.EqualTo(200), "仓库应提升容量");
            yield return null;

            var warehouseRenderer = FindTileRenderer(buildSpot.Value);
            Assert.That(warehouseRenderer, Is.Not.Null, "应能找到仓库渲染物体");
            Assert.That(
                warehouseRenderer.color,
                Is.EqualTo(new Color(0.62f, 0.45f, 0.28f)),
                "仓库应显示棕色");

            var woodBefore = loop.Pool.GetAmount(ResourceType.Wood);
            Assert.That(
                DemolishService.TryDemolish(loop.Map, loop.Pool, loop.ActionPoints, buildSpot.Value),
                Is.True,
                "应能拆除仓库");
            Assert.That(loop.Map.GetTile(buildSpot.Value).Building, Is.EqualTo(BuildingType.None), "拆除后格子应空出");
            Assert.That(loop.Pool.Capacity, Is.EqualTo(ResourcePool.DefaultCapacity), "容量应回落");
            Assert.That(loop.Pool.GetAmount(ResourceType.Wood), Is.EqualTo(woodBefore + 5), "应返还一半木头");
            yield return null;

            var grassRenderer = FindTileRenderer(buildSpot.Value);
            Assert.That(grassRenderer, Is.Not.Null);
            Assert.That(
                grassRenderer.color,
                Is.EqualTo(terrainColorBefore),
                "拆除后格子应恢复地形色（BuildingRemoved 事件接线）");
        }

        private static GridPos? FindResourceTile(GridMap map, TerrainType terrain)
        {
            for (var x = 0; x < map.Width; x++)
            {
                for (var y = 0; y < map.Height; y++)
                {
                    var tile = map.GetTile(new GridPos(x, y));
                    if (tile.Terrain == terrain && tile.ResourceAmount > 0 && tile.Building == BuildingType.None)
                    {
                        return new GridPos(x, y);
                    }
                }
            }

            return null;
        }

        private static GridPos? FindBuildableTile(GridMap map)
        {
            for (var x = 0; x < map.Width; x++)
            {
                for (var y = 0; y < map.Height; y++)
                {
                    var tile = map.GetTile(new GridPos(x, y));
                    if (tile != null && tile.Building == BuildingType.None && tile.IsWalkable)
                    {
                        return new GridPos(x, y);
                    }
                }
            }

            return null;
        }

        private static bool ConfirmBaseAtWalkable(GameLoop loop)
        {
            var spot = FindBuildableTile(loop.Map);
            return spot.HasValue && loop.ConfirmBase(spot.Value);
        }

        private static SpriteRenderer FindTileRenderer(GridPos pos)
        {
            var gridView = Object.FindAnyObjectByType<GridView>();
            if (gridView == null)
            {
                return null;
            }

            var grid = gridView.transform.Find("Grid");
            if (grid == null)
            {
                return null;
            }

            for (var i = 0; i < grid.childCount; i++)
            {
                var child = grid.GetChild(i);
                if (Mathf.FloorToInt(child.localPosition.x) == pos.X &&
                    Mathf.FloorToInt(child.localPosition.y) == pos.Y)
                {
                    return child.GetComponent<SpriteRenderer>();
                }
            }

            return null;
        }
    }
}
