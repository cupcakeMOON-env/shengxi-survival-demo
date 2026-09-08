# 生息演算 Like 生存建造 Demo

2D 俯视网格生存建造小游戏（Unity 6000.4.9f1）。白天采集资源、建造防御，夜晚敌人波次进攻据点；撑过 7 天胜利，据点被摧毁则失败。约 10~15 分钟一局，为简历准备的程序向 Demo。

## 运行

- Unity Hub 打开 `D:\unity\ShengXiSurvivalDemo`（编辑器版本 6000.4.9f1）
- 打开 `Assets/Scenes/Main.unity`（首次加载会自动创建）
- 按 Play

## 通过 npm 游玩（Windows）

不打开 Unity 也能玩已打包版本：

```bash
npm install -g github:cupcakeMOON-env/rts-demo
rts-demo
```

安装时包装脚本会从 GitHub Release 自动下载游戏（约 33MB）到本机，之后 `rts-demo` 命令直接启动游戏。

## 操作

| 操作 | 说明 |
|---|---|
| 左键点击 | 开局选址：直接落定据点（金色=可放/红色=不可）；白天：建造模式下放置建筑、拆除模式下拆除 |
| 底部菜单 | 选择建筑、拆除、进入夜晚、存档、读档（悬停红/绿预览） |
| 进入夜晚 | 刷出当晚敌人，自动战斗，清场后回白天、行动点重置 |

## 玩法规则

- 3 种资源：木头、石头、食物；开局自带木头20、石头15；资源靠采集站自动采集（手动点击采集已移除）；容量默认 100，仓库 +100，仓库被拆/被摧毁时容量回落
- 行动点每天 10 点，建造消耗，每晚结束自动重置；拆除不消耗行动点
- 5 种建筑：采集站（建在资源格上自动采集）、仓库（容量+100）、围墙（最厚的阻挡建筑）、箭塔（曼哈顿射程 3、伤害 1，数值在 BuildingCatalog）、工坊（占位）
- 食物供给：箭塔夜晚战斗期间每秒消耗 1 食物（BuildingDef.FoodPerSecond）；食物不足时箭塔断粮停火（格子变灰），恢复供给后自动复工
- 建筑血量（BuildingDef.MaxHp）：围墙 10，采集站/仓库/箭塔/工坊 5；敌人贴身攻击，归零即摧毁并撤销效果
- 拆除建筑：白天任意拆（据点除外）、返还 50% 造价、仓库容量加成同步撤销
- 建筑落格后格子变为对应颜色：采集站青绿、仓库棕、围墙灰、箭塔蓝、工坊橙、据点金
- 敌人每 0.5 秒行动一次（BFS 寻路，建筑与水面不可通行）：优先锁定最近的建筑（曼哈顿距离，据点除外）并贴身攻击，摧毁后找下一个；场上没有其他建筑时才进攻据点，到达据点扣血并消失
- 据点 20 血，开局由玩家自选位置，刷怪点会随据点位置自适应
- 波次随天数增强，第 7 天 Boss 波

## 架构

```text
┌────────────────────────────────────────────────────────────┐
│  View / UI（MonoBehaviour 表现层）                          │
│  GridView  EnemyView  BuildModeController  TileClickInput   │
│  ResourceBar  DayBanner  BuildMenu  GameOverPanel           │
└───────────────┬───────────────────────────────▲────────────┘
                │ 订阅事件                      │ 调用服务
┌───────────────▼───────────────────────────────┴────────────┐
│  Simulation（纯 C#，不依赖 Unity，可单测/可脱离 Unity 编译） │
│  GridMap/Tile  ResourcePool  ActionPointSystem  DayCycle    │
│  BuildService  DemolishService  CollectService  CombatSim   │
│  CollectorSystem  FoodSupplySystem  WaveScheduler  Pathfinding  SaveMigrator │
│  SaveSerializer  SaveData  BuildingDef  BuildingCatalog     │
└───────────────┬─────────────────────────────────────────────┘
                │ 数据驱动
┌───────────────▼─────────────────────────────────────────────┐
│  Data：BuildingCatalog 建筑定义（可迁移为 ScriptableObject） │
└─────────────────────────────────────────────────────────────┘
```

**数据流**：输入（点击/按钮）→ Simulation 服务 → 状态变更 → `GameEvents` 事件 → View/UI 刷新。UI 与 View 只读模拟数据，绝不直接修改；模拟层不持有任何 GameObject 引用。

**目录结构**：`Assets/Scripts/Core`（引导与循环）、`Simulation`（纯逻辑）、`View`（表现）、`UI`（界面）、`Save`（预留）、`Assets/Tests/Editor`（EditMode 测试）。

## 测试

- EditMode：Window → General → Test Runner → EditMode → Run All（当前 104 项）
- PlayMode 冒烟：Test Runner → PlayMode → Run All（当前 5 项；覆盖运行时装配层：引导、日夜循环、存读档、拆除、按钮可见性）
- 模拟层脱离 Unity 独立验证：`dotnet run --project C:\Users\林好\ShengXiSimulationVerify\SimulationVerify.csproj`

## 设计取舍 FAQ

**为什么模拟层不用 MonoBehaviour？**
三个原因：确定性（模拟只依赖输入和自身状态，不依赖 Unity 生命周期）、可测试性（纯 C# 类可以在 EditMode 下单测，甚至可以脱离 Unity 编译运行）、存档简单（序列化的是数据而不是对象引用）。

**为什么用事件总线而不是每帧轮询？**
资源变化、行动点变化、敌人移动都通过 `GameEvents` 通知，UI/View 只在收到事件时刷新，避免每帧遍历、避免 UI 与模拟耦合。

**为什么地图逻辑不用 Unity 的 Tilemap？**
Tilemap 绑定 GameObject 与生命周期，难测试、难存档；`GridMap` 是纯 C# 二维数组，可单测、可干净地序列化，Tilemap/渲染层只是它的投影。

**存档怎么做版本兼容？**
存档只存 ID、坐标、数值（`SaveData`），用 `version` 字段 + `SaveMigrator.Upgrade()` 做迁移；任何字段变更都必须升版本并补迁移逻辑，禁止原地改旧数据。

**随机地图怎么保证可复现？**
`MapGenerator.CreateRandomMap(seed)` 用确定性 Value Noise + fBm 生成，同一 seed 永远得到同一张图，存档里保存 seed，方便调试与回放。

## 里程碑

| 里程碑 | 内容 | 状态 |
|---|---|---|
| M0 | 工程骨架、分层架构、网格显示 | ✅ |
| M1 | 资源系统、资源栏（手动采集后移除，改由采集站产出） | ✅ |
| M2 | 行动点、建造系统、红绿预览、采集站 | ✅ |
| M3 | 日夜循环、敌人波次、塔防、据点、胜负 | ✅ |
| M4 | 随机地图、结算面板、重新开始 | ✅ |
| M5 | 存档/读档、版本迁移、测试、README | ✅ |
| 后续迭代 | 拆除建筑、敌人优先攻击建筑、建筑血量、存档 v2、按钮条可见性修复 | ✅ |
