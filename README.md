# 生息演算 Like 生存建造 Demo

2D 俯视网格生存建造小游戏（Unity 6000.4.9f1 / C#）。从开始界面进入，白天用行动点采集与建造，夜晚抵御敌人波次进攻据点；撑过 7 天胜利，据点被摧毁则失败，也可以随时点「结算本局」主动收局看结算。单局约 10~15 分钟。

这是一个**程序向 Demo**：重点不在美术表现，而在模拟层与表现层分离、事件驱动、数据驱动、版本化存档、三层自动化测试这套工程做法。当前进度：M0~M5 全部完成，并已迭代到 M12（工坊多配方生产链、建筑升级、修理系统、工坊手动控制面板、主动结算与结算统计，存档 v5）。

## 亮点

| 能力 | 实现 |
|---|---|
| 纯 C# 模拟层 | `Assets/Scripts/Simulation` 程序集 `noEngineReferences: true`，不引用 UnityEngine，可脱离 Unity 编译与测试 |
| 事件驱动表现层 | UI / View 订阅 `GameEvents`（资源、建造、受击、升级、修理、供给变化…），不轮询、不直接改模拟数据 |
| 数据驱动数值 | 建筑、配方、波次数值集中在 `BuildingCatalog` / `CraftingCatalog` / `WaveScheduler`，调数值不动逻辑 |
| 建造经营循环 | 采集站自动采集 → 工坊多配方并行生产（建材 / 修理包）→ 建材升级箭塔与围墙 |
| 建造与维护 | 建造（红 / 绿预览）、拆除（返还 50%，不足 1 的部分向上取整）、升级（最高 3 级）、修理（1 修理包 + 1 行动点修满） |
| 工坊手动控制 | 每座工坊独立的总开关（开工 / 停产）+ 逐条配方启停（`WorkshopService`）；面板实时显示每条产线为何没在跑（缺原料 / 容量满 / 已关闭）与每秒收支 |
| 塔防战斗 | 曼哈顿射程判定 + BFS 寻路索敌、建筑血量、受击闪白与血条、箭塔夜晚食物供给（断粮停火 / 恢复复工） |
| 随机地图 | 确定性 Value Noise + fBm，同 seed 同地图；据点放置与刷怪点连通性校验，杜绝「敌人到不了据点」死局 |
| 版本化存档 | `SaveData` DTO + `SaveMigrator`，v1 → v4 逐版迁移 |
| 三层测试 | EditMode 136 项 + PlayMode 6 项冒烟 + 模拟层脱离 Unity 独立验证 |

## 快速开始

### 方式一：不装 Unity 直接玩（Windows）

```bash
npm install -g https://codeload.github.com/cupcakeMOON-env/shengxi-survival-demo/tar.gz/v1.1.0
shengxi-demo
```

安装时包装脚本会从 GitHub Release 下载约 33MB 的 Windows 独立包到本机，之后 `shengxi-demo` 命令直接启动游戏。仓库根目录的 `package.json` / `bin/` / `scripts/` 就是这套 npm 包装（它不参与 Unity 工程编译）。

### 方式二：用 Unity 打开工程

1. Unity Hub 用 **6000.4.9f1** 打开本仓库根目录
2. 打开 `Assets/Scenes/Main.unity`（首次打开会自动生成）
3. 按 Play

## 操作

| 操作 | 说明 |
|---|---|
| 左键点击 | 开局：落定据点（金色=可放 / 红色=不可放）；白天：建造模式放置建筑、拆除模式拆除、修理模式修满目标；没有操作模式时点建筑会开面板（工坊产线 / 箭塔围墙升级） |
| 鼠标右键 | 退出当前操作模式（建造 / 拆除 / 修理），等价于再点一次那个按钮；没有模式时按了也没影响 |
| 开始界面 | 开机先进开始界面：开始新游戏 / 读档 / 退出游戏。读档会列出三个存档位（带天数、昼夜与时间） |
| 底部按钮条 | 采集站 / 仓库 / 围墙 / 箭塔 / 工坊 / 拆除 / 修理 / 进入夜晚 / 存档 / 读档 / 结算本局 |
| 点击箭塔 / 围墙 | 打开升级面板：写清这一级要多少建材与行动点、手上现有多少、升完伤害与血量变成多少，确认后才升级（升级不再有独立模式按钮） |
| 存 / 读档 | 游戏内的「存档 / 读档」会打开同一套三槽位面板；打开期间模拟暂停（避免选存档时被敌人打死） |
| 点击工坊格子 | 打开工坊面板：整体开工 / 停产、逐条配方开关、每秒收支与停产原因。处于建造 / 拆除 / 升级 / 修理模式时也照样能点开（这些模式下点击工坊本来也执行不成别的操作），只有「没有操作模式时点其他格子」才会收起面板；白天夜晚都能看，生产仍只在白天 |
| 结算本局 | 按钮条最右侧：主动结束本局并弹出结算页，显示天数、据点血量、建筑明细与剩余资源；已结束的局不能重复结算 |
| 悬停预览 | 建造、拆除、升级、修理均有红 / 绿判定预览；选中箭塔或建造箭塔时显示曼哈顿射程范围 |
| 进入夜晚 | 刷出当晚敌人并自动战斗，清场后回到白天、行动点重置 |

## 玩法规则

**资源与行动点**

- 5 种资源：木头、石头、食物、建材、修理包；开局自带木头 20、石头 15；资源只由采集站产出（手动点击采集已移除）
- 容量默认 100，每座仓库 +100；仓库被拆或被摧毁时容量回落，超出新上限的资源截断
- 每天 10 行动点，建造 / 升级 / 修理消耗，夜晚结束自动重置；拆除不消耗行动点

**建筑**

| 建筑 | 造价 | 行动点 | 作用 |
|---|---|---|---|
| 采集站 | 木 5 | 1 | 建在资源格上，每秒自动采集 1；只要木材（见下方 FAQ：避免资源死锁） |
| 仓库 | 木 10 + 石 5 | 2 | 资源容量 +100 |
| 围墙 | 木 2 | 1 | 阻挡敌人通行，血量最厚（10），专为挡线 |
| 箭塔 | 木 15 + 石 10 | 3 | 曼哈顿射程 3、伤害 1；夜晚每秒消耗 1 食物 |
| 工坊 | 木 8 + 石 6 | 2 | 白天并行生产建材与修理包，可手动停产或逐条关线 |
| 据点 | 开局选址 | — | 20 血，被摧毁判负；可用 1 修理包 + 1 行动点修满 |

血量（`BuildingDef.MaxHp`）：围墙 10，采集站 / 仓库 / 箭塔 / 工坊 5。落格后格子显示独立颜色——采集站青绿 / 仓库棕 / 围墙灰 / 箭塔蓝 / 工坊橙 / 据点金，建筑等级越高颜色越亮。

**生产、升级与修理**

- 工坊两条配方**并行**结算：木 1 + 石 1 / 秒 → 建材 1；木 1 + 食物 1 / 秒 → 修理包 1（`CraftingCatalog.RecipeDef` 通用原料列表，每条产线独立判定原料与产物容量，只白天生产）
- 手动控制：每座工坊有独立的开工 / 停产总开关，外加逐条配方的启停（`WorkshopService`，默认全开）。被停掉的产线既不消耗原料也不产出，开关随存档保存（v5）。面板上的「原料不足 / 容量已满 / 已关闭」与结算走同一套判定（`ProductionSystem.EvaluateRecipe`），所以提示和实际行为不会对不上
- 升级：点建筑开面板确认后才升级，消耗「建材 + 1 行动点」，最高 3 级。箭塔 6 建材 / 级，每级伤害 +1（等级 1/2/3 血量 5 / 8 / 11）；围墙 4 建材 / 级，每级血量 +5（10 / 15 / 20）。升级同时整修至新等级满血，所以白天也可以用建材顺手修残血建筑
- 拆除：白天任意拆（据点除外），按 50% 返还造价并含已投入的升级建材，**不足 1 的部分向上取整**（木 5 的 50% = 2.5 → 返 3）；返还同样受仓库容量限制，满了会被截断。被敌人摧毁不返还任何资源
- 修理：白天消耗 1 修理包 + 1 行动点，把残血建筑或据点直接修满（`RepairService`），不可升级的仓库 / 采集站 / 工坊也靠这条规则恢复

**敌人与战斗**

- 敌人每 0.5 秒行动一次，建筑格与水面不可通行；按 **BFS 路径距离**锁定最近的建筑（绕开障碍而不是直线距离），贴身攻击直到摧毁再换下一个；场上没有其他建筑时才进攻据点，到达据点扣血并消失
- 箭塔每个战斗 tick 先于敌人行动结算开火；开火前检查食物供给状态
- 波次：第 1~6 天普通波（数量 `min(2+day, 8)`、血量 `1+day/2`、伤害 1），第 7 天 Boss 波（6 只、血量 `8+day*2`、伤害 2）
- 刷怪点随据点位置自适应，取据点所在行 / 列四条边上的可走格；某条边整体连不通时改用可达边，且据点选址必须连通地图边缘

**随机地图**

- 30×30，海拔噪声定水 / 陆地，湿度噪声定森林，再聚类点缀石矿与浆果丛；同一 seed 永远同一张图，存档保存 seed
- 据点放置校验用 BFS 要求连通地图边缘（孤立水盆 / 小岛显示红色不可放），配合刷怪点兜底，任何随机地图都不会出现敌人到不了据点的死局

## 架构

```text
┌────────────────────────────────────────────────────────────┐
│  View / UI（MonoBehaviour 表现层）                          │
│  GridView  EnemyView  BuildModeController  TileClickInput   │
│  ResourceBar  DayBanner  BuildMenu  WorkshopPanel  GameOverPanel  GameOverText │
│  StartMenuPanel（开始界面 + 三存档位面板）                                   │
│  UpgradePanel（建筑升级面板，与 WorkshopPanel 各自认领自己那类建筑）          │
└───────────────┬───────────────────────────────▲────────────┘
                │ 订阅事件                      │ 调用服务
┌───────────────▼───────────────────────────────┴────────────┐
│  Simulation（纯 C#，不依赖 Unity，可单测 / 可脱离 Unity 编译）│
│  GridMap/Tile  ResourcePool  ActionPointSystem  DayCycle    │
│  BuildService  DemolishService  CollectService  CombatSim   │
│  CollectorSystem  ProductionSystem  FoodSupplySystem  WaveScheduler         │
│  Pathfinding  UpgradeService  RepairService  BuildingStats  CraftingCatalog │
│  SaveSerializer  SaveData  SaveSlots  SettlementService  RunOutcome  WorkshopService │
└───────────────┬─────────────────────────────────────────────┘
                │ 数据驱动
┌───────────────▼─────────────────────────────────────────────┐
│  Data：BuildingCatalog / CraftingCatalog 数值定义            │
└─────────────────────────────────────────────────────────────┘
```

**数据流**：输入（点击 / 按钮）→ Simulation 服务 → 状态变更 → `GameEvents` 事件 → View / UI 刷新。UI 与 View 只读模拟数据，绝不直接修改；模拟层不持有任何 GameObject 引用。

**目录结构**

```text
Assets/
  Scripts/
    Simulation/     # 纯 C# 模拟层（noEngineReferences）
                    # GridMap Tile GridPos ResourcePool ActionPointSystem DayCycle Base Enemy
                    # WaveScheduler Pathfinding CombatSim FoodSupplySystem BuildService DemolishService
                    # CollectorSystem CollectService SaveData SaveSerializer SaveMigrator
                    # ProductionSystem CraftingCatalog UpgradeService BuildingStats RepairService
                    # MapGenerator Noise BuildingDef BuildingCatalog BasePlacementValidator GameEvents
    Core/           # GameBootstrap 自举、GameLoop 主循环、SaveService 存档 IO
    View/           # GridView EnemyView BuildModeController TileClickInput
                    # BasePlacementController TowerRangeOverlay
    UI/             # ResourceBar DayBanner BuildMenu GameOverPanel（运行时纯代码搭建）
    Save/ Data/     # 预留
  Editor/           # ProjectSetup（自动创建 Main.unity）、BuildScript（命令行打 Windows 包）
  Scenes/Main.unity
  Tests/
    Editor/         # 136 项 EditMode 测试
    PlayMode/       # 6 项冒烟测试
package.json bin/ scripts/   # npm 包装：从 GitHub Release 下载并启动独立包
```

**程序集划分**：`ShengXi.Simulation`（纯逻辑，`noEngineReferences: true`）、`ShengXi.Game`（Core / View / UI，引用 Simulation）、测试程序集 `Tests/Editor` 与 `Tests/PlayMode`。

## 存档与版本迁移

| 版本 | 新增内容 |
|---|---|
| v1 | 基础存档：资源、天数、据点、建筑列表 |
| v2 | 建筑血量 |
| v3 | 建筑等级（旧档视为 1 级）+ 建材 |
| v4 | 修理包（旧档为 0） |
| v5 | 工坊开工状态与配方开关（旧档迁移为「开工 + 配方全开」） |

固定三个存档位（`SaveSlots`），槽位 Id 就是文件名：`shengxi_slot1.json` / `shengxi_slot2.json` / `shengxi_slot3.json`，都写在 `Application.persistentDataPath` 下（`SaveSerializer.Build/Rebuild*` 负责打包还原，`SaveService` 负责 IO 与槽位概况，`SaveMigrator.Upgrade()` 负责迁移）。字段变更必须升版本并补迁移，禁止原地改旧数据。读档后 `GameLoop.LoadGame` 会统一重发事件，视图自动重建。

开始界面阶段**不存在世界**：`GameLoop.Awake` 只登记实例，地图 / 资源 / 日夜都由 `NewGame` / `LoadGame` 创建；`ReturnToMenu()` 会把它们清空并广播 `MapInitialized(null)`，各视图据此收干净画面，开始界面也正是靠这个事件显示自己。

## 测试

| 层 | 规模 | 怎么跑 |
|---|---|---|
| EditMode | 165 项 | Unity → Window → General → Test Runner → EditMode → Run All |
| PlayMode | 15 项冒烟 | Test Runner → PlayMode → Run All（覆盖引导、日夜循环、存读档、拆除、修理、按钮可见性、工坊面板、升级面板、模式取消、建造模式下点开工坊、格子坐标换算、结束横幅、主动结算、开始界面与三存档位） |
| 模拟层独立验证 | — | 脱离 Unity 的独立 harness：`dotnet run --project <SimulationVerify 工程>` |

命令行跑测试（**先关闭 Unity 编辑器**，否则报 Multiple Unity instances）：

```powershell
& "<Unity 编辑器路径>\Unity.exe" test "<仓库路径>" --mode EditMode --output "<仓库路径>\test-results.xml" --timeout 600
& "<Unity 编辑器路径>\Unity.exe" test "<仓库路径>" --mode PlayMode --output "<仓库路径>\playmode-results.xml" --timeout 900
```

当前基线：EditMode 136/136、PlayMode 6/6、模拟层独立验证通过。

## 构建 Windows 独立包

```powershell
& "<Unity 编辑器路径>\Unity.exe" -batchmode -nographics -quit -accept-apiupdate `
  -projectPath "<仓库路径>" -executeMethod ShengXi.EditorTools.BuildScript.BuildWindows
```

也可以从编辑器菜单 `Tools/ShengXi/Build Windows` 触发。产物目录由 `Assets/Editor/BuildScript.cs` 里的 `OutputDir` 决定（默认 `D:/codex/ShengXiSurvivalDemo-Win64`），运行 exe 不需要 Unity 编辑器。

## 里程碑

| 里程碑 | 内容 | 状态 |
|---|---|---|
| M0 | 工程骨架、分层架构、网格显示 | ✅ |
| M1 | 资源系统、资源栏（后改为采集站自动产出） | ✅ |
| M2 | 行动点、建造系统、红 / 绿预览、采集站 | ✅ |
| M3 | 日夜循环、敌人波次、塔防、据点、胜负 | ✅ |
| M4 | 随机地图、结算面板、重新开始 | ✅ |
| M5 | 存档 / 读档、版本迁移、测试、README | ✅ |
| 后续迭代 | 拆除建筑、敌人优先攻击建筑、建筑血量、存档 v2、UI 布局与按钮条修复 | ✅ |
| M9 | 工坊生产链（木 + 石 → 建材）、建筑升级（箭塔 / 围墙）、存档 v3 | ✅ |
| M10 | 多配方并行（+ 修理包）、修理建筑与据点、存档 v4 | ✅ |
| M11 | 工坊手动控制：总开关 + 逐条配方启停 + 控制面板、存档 v5 | ✅ |
| M12 | 主动结算：`RunOutcome` 三态结局 + 结算统计页 + 结束横幅修复 | ✅ |
| M13 | 开始界面（新游戏 / 读档 / 退出）+ 三存档位面板 + 结算页返回开始界面 | ✅ |
| M14 | 建筑升级面板：点建筑显示材料与效果并确认升级，去掉「升级」模式按钮 | ✅ |

## 已知限制与下一步

- 配方扩展第二阶段未做：精钢（石 2 + 建材 1）→ 满级塔 / 墙「改造」，或新增「重弩塔」
- 数值平衡未精调：采集站造价 / 产出、工坊产线速率、修理包消耗、升级成本、波次强度、据点血量
- PlayMode 测试未模拟真实鼠标输入，颜色与视觉反馈目前靠手动确认；敌人血条、建筑毁坏表现、受击音效未做
- 箭塔建造预览的射程高亮未跟随红 / 绿判定（工坊停产原因已在面板里给出）
- `BuildingDef` 目前是代码常量，可迁移到 ScriptableObject 做更彻底的数据驱动
- 60~90 秒演示视频待录制

## 设计取舍 FAQ

**为什么模拟层不用 MonoBehaviour？**
确定性（模拟只依赖输入与自身状态，不依赖 Unity 生命周期）、可测试性（纯 C# 类可在 EditMode 下单测，甚至脱离 Unity 编译运行）、存档简单（序列化数据而不是对象引用）。

**为什么用事件总线而不是每帧轮询？**
资源变化、行动点变化、敌人移动都通过 `GameEvents` 通知，UI / View 只在收到事件时刷新，避免每帧遍历、避免 UI 与模拟耦合。

**为什么地图逻辑不用 Unity 的 Tilemap？**
Tilemap 绑定 GameObject 与生命周期，难测试、难存档；`GridMap` 是纯 C# 二维数组，可单测、可干净序列化，渲染层只是它的投影。

**存档怎么做版本兼容？**
存档只存 ID、坐标、数值（`SaveData`），用 `version` 字段 + `SaveMigrator.Upgrade()` 做迁移。

**随机地图怎么保证可复现？**
`MapGenerator.CreateRandomMap(seed)` 用确定性 Value Noise + fBm 生成，同一 seed 永远得到同一张图，存档保存 seed，方便调试与复现问题。

**围墙为什么要有「必须最厚」的单测？**
否则玩家会拿功能建筑（采集站 / 工坊）当墙用，防御建筑的定位就失效了；`Wall_IsTheSturdiestBuilding` 这条测试守护这个平衡前提。

**为什么采集站只要木材，不要石头？**
采集站是唯一的资源来源（手动点击采集已移除）。如果它需要石头，就会出现「石头花光 → 造不出采集站 → 更采不到石头」的死锁，整局再也翻不了身。木材开局自带 20、且森林格在地图上分布最广，是唯一能托底的资源；`Collector_CostsWoodOnly` 与 `Collector_CanBeBuiltWithoutStone_SoTheGameCanAlwaysRecover` 两条测试守住这个前提。其余建筑照旧要石头——石头现在永远可得了。

## 许可

仅供个人作品与求职演示使用，`package.json` 标为 `UNLICENSED`；仓库未附开源许可文件，如需复用请先联系作者。
