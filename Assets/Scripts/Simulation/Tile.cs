using System;
using System.Collections.Generic;

namespace ShengXi.Simulation
{
    public enum TerrainType
    {
        Grass = 0,
        Forest = 1,
        Stone = 2,
        Water = 3,
        Bush = 4,
    }

    public enum BuildingType
    {
        None = 0,
        Collector = 1,   // 采集站
        Warehouse = 2,   // 仓库
        Wall = 3,        // 围墙
        ArrowTower = 4,  // 箭塔
        Workshop = 5,    // 工坊
        Base = 6,        // 据点
    }

    /// <summary>单个格子的数据：地形、资源余量、建筑、可通行性。</summary>
    public class Tile
    {
        public TerrainType Terrain { get; set; }
        public int ResourceAmount { get; set; }
        public BuildingType Building { get; set; }
        /// <summary>建筑当前血量；无建筑时为 0。敌人攻击建筑时扣减，归零即摧毁。</summary>
        public int BuildingHp { get; set; }
        /// <summary>
        /// 防御建筑是否因食物耗尽而断粮停火（瞬态：不存档，读档后由 FoodSupplySystem 重算）。
        /// </summary>
        public bool BuildingStarved { get; set; }

        /// <summary>
        /// 建筑等级：建造为 1；箭塔/围墙可经 UpgradeService 消耗建材升级（等级影响伤害/血量）。
        /// 无建筑时为 0；随存档 v3 持久化。
        /// </summary>
        public int BuildingLevel { get; set; }

        /// <summary>
        /// 工坊是否开工（玩家手动开关，默认开工）。停产时该工坊不消耗原料也不产出。
        /// 随存档 v5 持久化。
        /// </summary>
        public bool ProductionEnabled { get; set; } = true;

        /// <summary>
        /// 被玩家单独关闭的配方 Id 集合（懒创建，为空表示全部启用）。
        /// 用配方 Id 而不是下标做键：以后在目录里增删或排序配方，都不会影响已存档的开关。
        /// </summary>
        private HashSet<string> _disabledRecipes;

        /// <summary>该配方在这座工坊里是否启用（空 Id 视为启用，容错用）。</summary>
        public bool IsRecipeEnabled(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
            {
                return true;
            }

            return _disabledRecipes == null || !_disabledRecipes.Contains(recipeId);
        }

        /// <summary>单独开关一条配方。</summary>
        public void SetRecipeEnabled(string recipeId, bool enabled)
        {
            if (string.IsNullOrEmpty(recipeId))
            {
                return;
            }

            if (enabled)
            {
                if (_disabledRecipes == null)
                {
                    return;
                }

                _disabledRecipes.Remove(recipeId);
                if (_disabledRecipes.Count == 0)
                {
                    _disabledRecipes = null;
                }

                return;
            }

            _disabledRecipes = _disabledRecipes ?? new HashSet<string>();
            _disabledRecipes.Add(recipeId);
        }

        /// <summary>被关闭的配方 Id（只读；没有关闭任何产线时为空）。</summary>
        public IReadOnlyCollection<string> DisabledRecipeIds =>
            _disabledRecipes ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        /// <summary>是否有产线被单独关闭。</summary>
        public bool HasDisabledRecipes => _disabledRecipes != null && _disabledRecipes.Count > 0;

        /// <summary>
        /// 回到出厂设置：开工、所有产线启用。
        /// 建筑被拆除 / 摧毁时调用，避免同一格子上新建的工坊继承上一座的开关。
        /// </summary>
        public void ResetProductionState()
        {
            ProductionEnabled = true;
            _disabledRecipes = null;
        }

        /// <summary>
        /// 敌人/单位能否通过（水面与建筑不可通行；据点例外，敌人可踏入据点发动攻击）。
        /// 建筑不可通行后，敌人只能贴着建筑攻击，符合塔防「先拆防御再打据点」的节奏。
        /// </summary>
        public bool IsWalkable => Terrain != TerrainType.Water &&
                                 (Building == BuildingType.None || Building == BuildingType.Base);

        public Tile(TerrainType terrain)
        {
            Terrain = terrain;
            ResourceAmount = 0;
            Building = BuildingType.None;
        }
    }
}
