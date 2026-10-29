using System.Collections;
using System.IO;
using NUnit.Framework;
using ShengXi.Core;
using ShengXi.Simulation;
using ShengXi.UI;
using ShengXi.View;
using UnityEngine;
using UnityEngine.UI;
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
                DemolishService.TryDemolish(loop.Map, loop.Pool, buildSpot.Value),
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

        [UnityTest]
        public IEnumerator Collector_KeepsBuildingColor_AfterResourceIsDepleted()
        {
            yield return null;
            var loop = GameLoop.Instance;
            loop.NewGame();
            yield return null;
            Assert.That(ConfirmBaseAtWalkable(loop), Is.True, "应能放置据点");

            var resourcePos = FindResourceTile(loop.Map, TerrainType.Forest);
            Assert.That(resourcePos.HasValue, Is.True, "地图上应有森林资源格");
            Assert.That(
                BuildService.TryBuild(
                    loop.Map,
                    loop.Pool,
                    loop.ActionPoints,
                    BuildingCatalog.Get(BuildingType.Collector),
                    resourcePos.Value),
                Is.True,
                "应能在森林资源格上建造采集站");

            var renderer = FindTileRenderer(resourcePos.Value);
            Assert.That(
                renderer.color,
                Is.EqualTo(new Color(0.30f, 0.75f, 0.55f)),
                "采集站建成后应显示专属青绿色");

            var remaining = loop.Map.GetTile(resourcePos.Value).ResourceAmount;
            for (var i = 0; i < remaining; i++)
            {
                Assert.That(
                    CollectService.TryCollect(loop.Map, loop.Pool, resourcePos.Value),
                    Is.True,
                    "采集中应持续成功");
            }

            Assert.That(
                loop.Map.GetTile(resourcePos.Value).Terrain,
                Is.EqualTo(TerrainType.Grass),
                "资源采空后地形应变为草地");
            Assert.That(
                loop.Map.GetTile(resourcePos.Value).Building,
                Is.EqualTo(BuildingType.Collector),
                "资源采空不应移除建筑");
            Assert.That(
                renderer.color,
                Is.EqualTo(new Color(0.30f, 0.75f, 0.55f)),
                "TerrainChanged 不应覆盖采集站的建筑颜色");
        }

        [UnityTest]
        public IEnumerator BuildMenu_AllButtonsVisibleOnScreen()
        {
            yield return null;
            GameLoop.Instance.NewGame();
            yield return null;

            var buildMenu = Object.FindAnyObjectByType<BuildMenu>();
            Assert.That(buildMenu, Is.Not.Null, "BuildMenu 应存在");
            var canvas = Object.FindAnyObjectByType<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(
                scaler.screenMatchMode,
                Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand),
                "画布必须整体可见（超宽屏下底部按钮条才不会被裁掉）");
            var barGo = GameObject.Find("BuildMenu");
            Assert.That(barGo, Is.Not.Null, "BuildMenu 按钮条容器应存在");
            var bar = barGo.transform;

            var buttonCount = 0;
            var lastRight = float.MinValue;
            foreach (Transform child in bar)
            {
                if (child.GetComponent<Button>() == null)
                {
                    continue;
                }

                buttonCount++;
                var corners = new Vector3[4];
                child.GetComponent<RectTransform>().GetWorldCorners(corners);
                Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(0f), $"{child.name} 左边界应可见");
                Assert.That(corners[2].x, Is.LessThanOrEqualTo(Screen.width), $"{child.name} 右边界应可见");
                Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(0f), $"{child.name} 底边界应可见");
                Assert.That(corners[1].y, Is.LessThanOrEqualTo(Screen.height), $"{child.name} 顶边界应可见");
                Assert.That(
                    corners[0].x,
                    Is.GreaterThanOrEqualTo(lastRight - 0.01f),
                    $"{child.name} 不应与前一按钮重叠（历史 bug：按钮未推进 x 坐标导致叠在一起）");
                lastRight = corners[2].x;
            }

            Assert.That(buttonCount, Is.GreaterThanOrEqualTo(9), "底部应有全部功能按钮");
        }

        [UnityTest]
        public IEnumerator Repair_Runtime_HealsBuildingAndBase()
        {
            yield return null;
            GameLoop.Instance.NewGame();
            yield return null;
            var loop = GameLoop.Instance;
            Assert.That(ConfirmBaseAtWalkable(loop), Is.True, "应能放置据点");

            var buildSpot = FindBuildableTile(loop.Map);
            Assert.That(buildSpot.HasValue, Is.True, "应有可建造位置");
            var warehousePos = buildSpot.Value;
            Assert.That(
                BuildService.TryBuild(loop.Map, loop.Pool, loop.ActionPoints,
                    BuildingCatalog.Get(BuildingType.Warehouse), warehousePos),
                Is.True,
                "应能建造仓库");

            loop.Map.SetBuildingHp(warehousePos, 2); // 模拟夜晚被打残
            loop.Pool.Add(ResourceType.RepairKit, 2);
            Assert.That(
                RepairService.TryRepairBuilding(loop.Map, loop.Pool, loop.ActionPoints, warehousePos),
                Is.True,
                "应能用修理包修满建筑");
            Assert.That(loop.Pool.GetAmount(ResourceType.RepairKit), Is.EqualTo(1), "应消耗 1 修理包");
            yield return null;
            Assert.That(
                loop.Map.GetTile(warehousePos).BuildingHp,
                Is.EqualTo(BuildingCatalog.Get(BuildingType.Warehouse).MaxHp),
                "建筑应回满血");

            loop.Base.TakeDamage(9);
            Assert.That(RepairService.TryRepairBase(loop.Base, loop.Pool, loop.ActionPoints), Is.True, "应能修满据点");
            Assert.That(loop.Base.CurrentHp, Is.EqualTo(loop.Base.MaxHp), "据点应回满血");
            Assert.That(loop.Pool.GetAmount(ResourceType.RepairKit), Is.Zero);
        }

        [UnityTest]
        public IEnumerator WorkshopPanel_SelectsWorkshopAndControlsProduction()
        {
            yield return null;
            GameLoop.Instance.NewGame();
            yield return null;
            var loop = GameLoop.Instance;
            Assert.That(ConfirmBaseAtWalkable(loop), Is.True, "应能放置据点");

            var buildSpot = FindBuildableTile(loop.Map);
            Assert.That(buildSpot.HasValue, Is.True, "应有可建造位置");
            Assert.That(
                BuildService.TryBuild(loop.Map, loop.Pool, loop.ActionPoints,
                    BuildingCatalog.Get(BuildingType.Workshop), buildSpot.Value),
                Is.True,
                "应能建造工坊");

            var panel = Object.FindAnyObjectByType<WorkshopPanel>();
            Assert.That(panel, Is.Not.Null, "应存在工坊面板组件");
            // 面板初始是隐藏的，GameObject.Find 只找激活对象，这里直接用组件持有的引用
            var panelGo = panel.PanelRoot;
            Assert.That(panelGo, Is.Not.Null, "面板容器应存在");
            Assert.That(panelGo.activeSelf, Is.False, "未选中工坊时面板应隐藏");

            // 实际点击由 TileClickInput 换算格子后发出这条事件，测试走同一条路径
            GameEvents.RaiseBuildingSelected(buildSpot.Value);
            yield return null;
            Assert.That(panel.IsOpen, Is.True, "选中工坊后面板应打开");
            Assert.That(panelGo.activeSelf, Is.True);
            Assert.That(panel.SelectedPosition, Is.EqualTo(buildSpot.Value));

            // 面板必须完整落在屏幕内（历史 bug：按钮条溢出被裁掉）
            var corners = new Vector3[4];
            panelGo.GetComponent<RectTransform>().GetWorldCorners(corners);
            Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(0f), "面板左边界应可见");
            Assert.That(corners[2].x, Is.LessThanOrEqualTo(Screen.width), "面板右边界应可见");
            Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(0f), "面板底边界应可见");
            Assert.That(corners[1].y, Is.LessThanOrEqualTo(Screen.height), "面板顶边界应可见");

            Assert.That(FindDeep(panelGo.transform, "Btn_Producing"), Is.Not.Null, "应有开工/停产总开关");
            Assert.That(FindDeep(panelGo.transform, "Btn_Recipe_material"), Is.Not.Null, "应有建材产线开关");
            Assert.That(FindDeep(panelGo.transform, "Btn_Recipe_repair-kit"), Is.Not.Null, "应有修理包产线开关");

            var tileRenderer = FindTileRenderer(buildSpot.Value);
            Assert.That(tileRenderer, Is.Not.Null, "应能找到工坊格子渲染物体");

            // 总开关：停产后既不吃原料也不产出，格子应变暗
            var woodBefore = loop.Pool.GetAmount(ResourceType.Wood);
            var stoneBefore = loop.Pool.GetAmount(ResourceType.Stone);
            panel.ToggleProducing();
            yield return null;
            Assert.That(loop.Map.GetTile(buildSpot.Value).ProductionEnabled, Is.False, "应切到停产");
            ProductionSystem.Tick(loop.Map, loop.Pool);
            Assert.That(loop.Pool.GetAmount(ResourceType.Wood), Is.EqualTo(woodBefore), "停产后不应消耗木头");
            Assert.That(loop.Pool.GetAmount(ResourceType.Stone), Is.EqualTo(stoneBefore), "停产后不应消耗石头");
            Assert.That(
                tileRenderer.color,
                Is.EqualTo(new Color(0.46f, 0.34f, 0.18f)),
                "停产的工坊应显示暗色");

            // 单条产线开关：重开总开关、关掉修理包线，建材线应照常
            loop.Pool.Add(ResourceType.Food, 30); // 开局食物为 0，先给足原料才能看出「关掉的产线不再消耗」
            var foodBefore = loop.Pool.GetAmount(ResourceType.Food);
            panel.ToggleProducing();
            panel.ToggleRecipe(CraftingCatalog.IdRepairKit);
            yield return null;
            Assert.That(
                loop.Map.GetTile(buildSpot.Value).IsRecipeEnabled(CraftingCatalog.IdRepairKit),
                Is.False,
                "修理包产线应被单独关闭");

            ProductionSystem.Tick(loop.Map, loop.Pool);
            Assert.That(loop.Pool.GetAmount(ResourceType.Material), Is.EqualTo(1), "建材线应照常生产");
            Assert.That(loop.Pool.GetAmount(ResourceType.RepairKit), Is.Zero, "修理包线应停产");
            Assert.That(loop.Pool.GetAmount(ResourceType.Food), Is.EqualTo(foodBefore), "关闭的产线不应再吃食物");

            // 全部恢复后回到工坊本色
            panel.ToggleRecipe(CraftingCatalog.IdRepairKit);
            yield return null;
            Assert.That(
                tileRenderer.color,
                Is.EqualTo(new Color(0.95f, 0.62f, 0.2f)),
                "恢复开工后应回到工坊本色");

            GameEvents.RaiseBuildingSelected(null);
            yield return null;
            Assert.That(panel.IsOpen, Is.False, "取消选择后面板应关闭");
            Assert.That(panelGo.activeSelf, Is.False);
        }

        /// <summary>按名字递归找子物体（面板里的按钮挂在 Rows 容器下，直接 Find 找不到）。</summary>
        [UnityTest]
        public IEnumerator WorkshopPanel_OpensEvenWhileBuildModeIsActive()
        {
            yield return null;
            GameLoop.Instance.NewGame();
            yield return null;
            var loop = GameLoop.Instance;
            Assert.That(ConfirmBaseAtWalkable(loop), Is.True, "应能放置据点");

            var spot = FindBuildableTile(loop.Map);
            Assert.That(spot.HasValue, Is.True, "应有可建造位置");
            var workshopDef = BuildingCatalog.Get(BuildingType.Workshop);
            Assert.That(
                BuildService.TryBuild(loop.Map, loop.Pool, loop.ActionPoints, workshopDef, spot.Value),
                Is.True,
                "应能建造工坊");

            var buildMode = Object.FindAnyObjectByType<BuildModeController>();
            var input = Object.FindAnyObjectByType<TileClickInput>();
            var panel = Object.FindAnyObjectByType<WorkshopPanel>();
            Assert.That(buildMode, Is.Not.Null);
            Assert.That(input, Is.Not.Null);
            Assert.That(panel, Is.Not.Null, "应存在工坊面板组件");
            Assert.That(panel.IsOpen, Is.False, "点击前面板应是收起的");

            // 复现玩家反馈的路径：造完工坊后建造模式仍然开着，这时点它原本悄无反应
            buildMode.SetActive(workshopDef);
            Assert.That(buildMode.ActiveDef, Is.Not.Null, "建造模式应处于激活状态");

            input.ResolveClick(spot.Value);
            yield return null;

            Assert.That(panel.IsOpen, Is.True, "建造模式下点击工坊也应打开面板");
            Assert.That(panel.SelectedPosition, Is.EqualTo(spot.Value));
            Assert.That(buildMode.ActiveDef, Is.Not.Null, "打开面板不应顺手退出建造模式");

            // 建造模式下点据点：动作必然失败、也不是工坊，不应把面板误关
            input.ResolveClick(loop.Base.Position);
            yield return null;
            Assert.That(panel.IsOpen, Is.True, "建造模式下点其他格子不应误关面板");

            // 退出建造模式后，点别的格子才收起面板
            buildMode.ClearActive();
            input.ResolveClick(loop.Base.Position);
            yield return null;
            Assert.That(panel.IsOpen, Is.False, "无模式时点其他格子应收起面板");
        }

        [UnityTest]
        public IEnumerator BuildMode_CancelExitsEveryMode()
        {
            yield return null;
            GameLoop.Instance.NewGame();
            yield return null;
            var loop = GameLoop.Instance;
            Assert.That(ConfirmBaseAtWalkable(loop), Is.True, "应能放置据点");

            var buildMode = Object.FindAnyObjectByType<BuildModeController>();
            Assert.That(buildMode, Is.Not.Null);
            Assert.That(buildMode.HasActiveMode, Is.False, "开局不应处于任何操作模式");

            // 建造模式：选中一个建筑后取消（鼠标右键走的就是 CancelActiveMode）
            buildMode.SetActive(BuildingCatalog.Get(BuildingType.Collector));
            Assert.That(buildMode.HasActiveMode, Is.True);
            buildMode.CancelActiveMode();
            Assert.That(buildMode.HasActiveMode, Is.False, "取消后不该还有模式");
            Assert.That(buildMode.ActiveDef, Is.Null, "取消后不该还留着选中的建筑");

            // 拆除模式
            buildMode.SetDemolishActive();
            Assert.That(buildMode.IsDemolishing, Is.True);
            buildMode.CancelActiveMode();
            Assert.That(buildMode.IsDemolishing, Is.False);

            // 修理模式
            buildMode.SetRepairActive();
            Assert.That(buildMode.IsRepairing, Is.True);
            buildMode.CancelActiveMode();
            Assert.That(buildMode.HasActiveMode, Is.False);

            // 没有模式时取消应当是安全的空操作
            buildMode.CancelActiveMode();
            Assert.That(buildMode.HasActiveMode, Is.False);

            // 取消之后点地图不该再建造
            var input = Object.FindAnyObjectByType<TileClickInput>();
            var spot = FindBuildableTile(loop.Map);
            Assert.That(spot.HasValue, Is.True, "应有可建造位置");
            input.ResolveClick(spot.Value);
            yield return null;
            Assert.That(
                loop.Map.GetTile(spot.Value).Building,
                Is.EqualTo(BuildingType.None),
                "退出模式后点击地图不应再放建筑");
        }

        [UnityTest]
        public IEnumerator UpgradePanel_ShowsCostAndUpgradesAfterConfirm()
        {
            yield return null;
            GameLoop.Instance.NewGame();
            yield return null;
            var loop = GameLoop.Instance;
            Assert.That(ConfirmBaseAtWalkable(loop), Is.True, "应能放置据点");

            var towerPos = FindBuildableTile(loop.Map);
            Assert.That(towerPos.HasValue, Is.True, "应有可建造位置");
            Assert.That(
                BuildService.TryBuild(loop.Map, loop.Pool, loop.ActionPoints,
                    BuildingCatalog.Get(BuildingType.ArrowTower), towerPos.Value),
                Is.True,
                "应能建造箭塔");
            loop.Pool.Add(ResourceType.Material, 20);

            var panel = Object.FindAnyObjectByType<UpgradePanel>();
            Assert.That(panel, Is.Not.Null, "应有升级面板");
            Assert.That(panel.IsOpen, Is.False, "未选中建筑时不该显示");

            // 升级模式按钮应该已经被移除，修理模式保留
            var buildMenu = GameObject.Find("BuildMenu");
            Assert.That(buildMenu, Is.Not.Null);
            Assert.That(FindDeep(buildMenu.transform, "Btn_升级"), Is.Null, "升级模式按钮应已移除");
            Assert.That(FindDeep(buildMenu.transform, "Btn_修理"), Is.Not.Null, "修理按钮应保留");

            // 选中箭塔 → 面板要一次说清"要什么、现在有多少、升完变什么"
            GameEvents.RaiseBuildingSelected(towerPos.Value);
            yield return null;
            Assert.That(panel.IsOpen, Is.True, "选中箭塔应弹出升级面板");
            Assert.That(panel.SelectedPosition, Is.EqualTo(towerPos.Value));
            Assert.That(panel.CostText, Does.Contain("建材 6"), "要写清升级需要多少建材");
            Assert.That(panel.CostText, Does.Contain("现有 20"), "要写清玩家现有多少建材");
            Assert.That(panel.CostText, Does.Contain("行动点 1"), "要写清升级消耗多少行动点");
            Assert.That(panel.StateText, Does.Contain("可以升级"));
            Assert.That(panel.EffectText, Does.Contain("伤害 1 → 2"), "箭塔升级要显示伤害变化");
            Assert.That(panel.EffectText, Does.Contain("血量 5 → 8"), "箭塔升级要显示血量变化");

            // 确认升级：等级 +1、建材 -6、行动点 -1
            var materialBefore = loop.Pool.GetAmount(ResourceType.Material);
            var apBefore = loop.ActionPoints.Current;
            Assert.That(panel.ConfirmUpgrade(), Is.True, "确认升级应成功");
            yield return null;
            Assert.That(loop.Map.GetTile(towerPos.Value).BuildingLevel, Is.EqualTo(2));
            Assert.That(loop.Pool.GetAmount(ResourceType.Material), Is.EqualTo(materialBefore - 6));
            Assert.That(loop.ActionPoints.Current, Is.EqualTo(apBefore - 1));
            Assert.That(panel.EffectText, Does.Contain("血量 8 → 11"), "面板应跟到下一级的变化");

            // 升到满级后要明确说清"不能再升"，并且确认按钮不再生效
            Assert.That(panel.ConfirmUpgrade(), Is.True, "第二次升级应成功");
            yield return null;
            Assert.That(loop.Map.GetTile(towerPos.Value).BuildingLevel, Is.EqualTo(UpgradeService.MaxLevel));
            Assert.That(panel.StateText, Does.Contain("已满级"));
            var materialAtMax = loop.Pool.GetAmount(ResourceType.Material);
            Assert.That(panel.ConfirmUpgrade(), Is.False, "满级后确认升级应无效");
            Assert.That(loop.Pool.GetAmount(ResourceType.Material), Is.EqualTo(materialAtMax), "满级不该再扣建材");

            // 选中工坊时升级面板要让位（工坊有自己的产线面板）
            GameEvents.RaiseBuildingSelected(null);
            yield return null;
            Assert.That(panel.IsOpen, Is.False, "取消选中应关闭升级面板");
        }

        [UnityTest]
        public IEnumerator StartMenu_StartsNewGameAndReturnsFromSettlement()
        {
            yield return null;
            var loop = GameLoop.Instance;
            var menu = Object.FindAnyObjectByType<StartMenuPanel>();
            Assert.That(loop, Is.Not.Null);
            Assert.That(menu, Is.Not.Null, "应有开始界面");
            Assert.That(menu.SlotCount, Is.EqualTo(3), "应有三个存档位");

            // 回到开始界面 = 世界清空 + 菜单显示（开机时走的就是这个状态）
            loop.ReturnToMenu();
            yield return null;
            Assert.That(loop.IsInMenu, Is.True);
            Assert.That(loop.HasWorld, Is.False, "开始界面阶段不该有世界");
            Assert.That(loop.Map, Is.Null);
            Assert.That(menu.IsOpen, Is.True, "开始界面应可见");

            // 开始新游戏
            menu.StartNewGame();
            yield return null;
            Assert.That(loop.IsInMenu, Is.False);
            Assert.That(loop.HasWorld, Is.True);
            Assert.That(loop.IsChoosingBase, Is.True, "开新局后应先选据点");
            Assert.That(menu.IsOpen, Is.False, "开新局后开始界面应收起");

            // 结算页上的两个按钮：先「再来一局」，再「返回开始界面」
            Assert.That(ConfirmBaseAtWalkable(loop), Is.True, "应能放置据点");
            Assert.That(loop.SettleRun(), Is.True, "应能主动结算");
            yield return null;

            var playAgain = GameObject.Find("Btn_再来一局");
            var backToMenu = GameObject.Find("Btn_返回开始界面");
            Assert.That(playAgain, Is.Not.Null, "结算页应有「再来一局」");
            Assert.That(backToMenu, Is.Not.Null, "结算页应有「返回开始界面」");

            playAgain.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(loop.IsInMenu, Is.False);
            Assert.That(loop.HasWorld, Is.True, "「再来一局」应直接开一局新的");
            Assert.That(loop.GameOver, Is.False);
            Assert.That(loop.IsChoosingBase, Is.True, "新一局要重新选据点");

            Assert.That(ConfirmBaseAtWalkable(loop), Is.True);
            Assert.That(loop.SettleRun(), Is.True);
            yield return null;
            backToMenu.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(loop.IsInMenu, Is.True, "「返回开始界面」应把世界清掉");
            Assert.That(loop.HasWorld, Is.False);
            Assert.That(menu.IsOpen, Is.True, "回到开始界面后菜单应显示");
        }

        [UnityTest]
        public IEnumerator SaveSlots_SaveToSecondSlotAndLoadItBack()
        {
            yield return null;
            var loop = GameLoop.Instance;
            var menu = Object.FindAnyObjectByType<StartMenuPanel>();
            var slotId = SaveSlots.IdAt(1);
            Assert.That(slotId, Is.Not.Null);

            try
            {
                loop.NewGame();
                yield return null;
                Assert.That(ConfirmBaseAtWalkable(loop), Is.True);

                var buildSpot = FindBuildableTile(loop.Map);
                Assert.That(buildSpot.HasValue, Is.True);
                Assert.That(
                    BuildService.TryBuild(loop.Map, loop.Pool, loop.ActionPoints,
                        BuildingCatalog.Get(BuildingType.Warehouse), buildSpot.Value),
                    Is.True,
                    "应能建造仓库");
                var woodSaved = loop.Pool.GetAmount(ResourceType.Wood);

                menu.OpenSlots(SlotPanelMode.Save);
                yield return null;
                Assert.That(menu.IsShowingSlots, Is.True, "应切到存档位页");
                Assert.That(menu.Mode, Is.EqualTo(SlotPanelMode.Save));
                Assert.That(loop.IsPaused, Is.True, "打开存档面板时应暂停模拟");

                menu.ActivateSlot(1);
                yield return null;
                Assert.That(SaveService.Exists(slotId), Is.True, "应写出第二个槽位的存档");
                var info = SaveService.Inspect(slotId);
                Assert.That(info.Exists, Is.True);
                Assert.That(info.Day, Is.EqualTo(1));
                Assert.That(info.IsNight, Is.False);

                menu.Hide();
                Assert.That(loop.IsPaused, Is.False, "关掉面板应恢复模拟");

                // 换一局，再把存档读回来
                loop.NewGame();
                yield return null;
                Assert.That(ConfirmBaseAtWalkable(loop), Is.True);
                Assert.That(
                    loop.Map.GetTile(buildSpot.Value).Building,
                    Is.Not.EqualTo(BuildingType.Warehouse),
                    "新开一局不该带着上一局的仓库");

                loop.LoadGame(slotId);
                yield return null;
                Assert.That(loop.IsInMenu, Is.False);
                Assert.That(loop.HasWorld, Is.True);
                Assert.That(
                    loop.Map.GetTile(buildSpot.Value).Building,
                    Is.EqualTo(BuildingType.Warehouse),
                    "读档应还原建筑");
                Assert.That(loop.Pool.GetAmount(ResourceType.Wood), Is.EqualTo(woodSaved), "读档应还原资源");
                Assert.That(loop.CurrentSlot, Is.EqualTo(slotId));
            }
            finally
            {
                SaveService.Delete(slotId);
            }
        }

        [UnityTest]
        public IEnumerator SettleRun_EndsTheRunAndShowsSummary()
        {
            yield return null;
            GameLoop.Instance.NewGame();
            yield return null;
            var loop = GameLoop.Instance;
            Assert.That(ConfirmBaseAtWalkable(loop), Is.True, "应能放置据点");

            // 建一座仓库，让结算统计里有东西可看
            var buildSpot = FindBuildableTile(loop.Map);
            Assert.That(buildSpot.HasValue, Is.True, "应有可建造位置");
            Assert.That(
                BuildService.TryBuild(loop.Map, loop.Pool, loop.ActionPoints,
                    BuildingCatalog.Get(BuildingType.Warehouse), buildSpot.Value),
                Is.True,
                "应能建造仓库");

            var panel = Object.FindAnyObjectByType<GameOverPanel>();
            var banner = Object.FindAnyObjectByType<DayBanner>();
            Assert.That(panel, Is.Not.Null);
            Assert.That(banner, Is.Not.Null);
            Assert.That(panel.IsVisible, Is.False, "开局不该显示结算页");
            Assert.That(loop.GameOver, Is.False);
            Assert.That(loop.Outcome, Is.Null, "未结束时不应有结局");

            Assert.That(loop.SettleRun(), Is.True, "应能主动结算");
            yield return null;

            Assert.That(loop.GameOver, Is.True, "主动结算后本局即结束");
            Assert.That(loop.Outcome, Is.EqualTo(RunOutcome.Settled));
            Assert.That(panel.IsVisible, Is.True, "应弹出结算页");
            Assert.That(panel.DetailText, Does.Contain("主动收局"), "结算页要说明这是主动收局");
            Assert.That(panel.StatsText, Does.Contain("建筑 1 座"), "统计里应有建筑数量");
            Assert.That(panel.StatsText, Does.Contain("仓库 1"), "统计里应有建筑明细");
            Assert.That(banner.ResultVisible, Is.True);
            Assert.That(banner.ResultText, Does.Contain("本局结算"));

            // 已经结束的局不能重复结算，结局也不该被改写
            Assert.That(loop.SettleRun(), Is.False, "结束时再结算应被拒绝");
            Assert.That(loop.Outcome, Is.EqualTo(RunOutcome.Settled));

            var buildMenu = GameObject.Find("BuildMenu");
            Assert.That(buildMenu, Is.Not.Null);
            Assert.That(
                FindDeep(buildMenu.transform, "Btn_结算本局"),
                Is.Not.Null,
                "底部按钮条上应有主动结算入口");
        }

        [UnityTest]
        public IEnumerator GameOverBanner_HidesOnNewGameAndMatchesPanelDayCount()
        {
            yield return null;
            GameLoop.Instance.NewGame();
            yield return null;
            var loop = GameLoop.Instance;
            Assert.That(ConfirmBaseAtWalkable(loop), Is.True, "应能放置据点");

            var banner = Object.FindAnyObjectByType<DayBanner>();
            var panel = Object.FindAnyObjectByType<GameOverPanel>();
            Assert.That(banner, Is.Not.Null, "应有顶部横幅");
            Assert.That(panel, Is.Not.Null, "应有结算面板");
            Assert.That(banner.ResultVisible, Is.False, "开局不该显示胜负横幅");

            // 模拟「第 7 天夜晚清场」：GameOver 事件带的是自增之后的天数 8
            GameEvents.RaiseGameOver(RunOutcome.Victory, WaveScheduler.WinDay + 1);
            yield return null;

            Assert.That(banner.ResultVisible, Is.True, "结束后应显示胜负横幅");
            Assert.That(
                banner.ResultText,
                Does.Contain($"第 {WaveScheduler.WinDay} 天"),
                "横幅天数必须是玩家实际撑过的天数");
            Assert.That(
                banner.ResultText,
                Does.Not.Contain($"第 {WaveScheduler.WinDay + 1} 天"),
                "横幅不该用自增之后的天数");
            Assert.That(
                panel.DetailText,
                Does.Contain($"第 {WaveScheduler.WinDay} 天"),
                "结算面板与横幅必须显示同一个天数");

            // 开新局 / 读档都会重发 MapInitialized，胜负横幅必须收起来
            GameEvents.RaiseMapInitialized(loop.Map);
            yield return null;
            Assert.That(banner.ResultVisible, Is.False, "读档或开新局后胜负横幅必须隐藏");
        }

        [UnityTest]
        public IEnumerator GridPicking_MatchesRenderedTiles()
        {
            yield return null;
            GameLoop.Instance.NewGame();
            yield return null;
            var loop = GameLoop.Instance;
            Assert.That(ConfirmBaseAtWalkable(loop), Is.True, "应能放置据点");

            // 格子画在 world=(x,y)、边长 0.96，所以整格（含左右半边的边缘内侧）都应选中它自己。
            // 这里如果退回 FloorToInt，pos.X - 0.47 会落到左边那格，测试立刻失败。
            var probes = new[]
            {
                new GridPos(0, 0),
                new GridPos(11, 15),
                new GridPos(7, 22),
                new GridPos(29, 29),
            };
            foreach (var pos in probes)
            {
                Assert.That(GridPicking.WorldToGrid(new Vector3(pos.X, pos.Y, 0f)), Is.EqualTo(pos), $"格心 {pos}");
                Assert.That(
                    GridPicking.WorldToGrid(new Vector3(pos.X - 0.47f, pos.Y, 0f)),
                    Is.EqualTo(pos),
                    $"{pos} 的左半边应仍选中自己（半格偏移回归）");
                Assert.That(GridPicking.WorldToGrid(new Vector3(pos.X + 0.47f, pos.Y, 0f)), Is.EqualTo(pos), $"{pos} 的右半边");
                Assert.That(GridPicking.WorldToGrid(new Vector3(pos.X, pos.Y - 0.47f, 0f)), Is.EqualTo(pos), $"{pos} 的下半边");
                Assert.That(GridPicking.WorldToGrid(new Vector3(pos.X, pos.Y + 0.47f, 0f)), Is.EqualTo(pos), $"{pos} 的上半边");
            }

            // 再用真实相机走一遍屏幕坐标：GridView 把格子放在哪，就应当能从那个屏幕点选回同一格
            var camera = Object.FindAnyObjectByType<Camera>();
            Assert.That(camera, Is.Not.Null);
            var probe = new GridPos(11, 15);
            var tileRenderer = FindTileRenderer(probe);
            Assert.That(tileRenderer, Is.Not.Null, "应能找到该格子的渲染物体");
            var tileCenter = tileRenderer.transform.position;

            Assert.That(
                GridPicking.ScreenToGrid(camera, camera.WorldToScreenPoint(tileCenter)),
                Is.EqualTo(probe),
                "屏幕坐标往返应回到同一格");
            Assert.That(
                GridPicking.ScreenToGrid(camera, camera.WorldToScreenPoint(tileCenter + new Vector3(-0.47f, 0f, 0f))),
                Is.EqualTo(probe),
                "点格子可见范围的左半边也应选中自己");
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
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
            for (var x = 0; x < loop.Map.Width; x++)
            {
                for (var y = 0; y < loop.Map.Height; y++)
                {
                    if (loop.ConfirmBase(new GridPos(x, y)))
                    {
                        return true;
                    }
                }
            }

            return false;
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
