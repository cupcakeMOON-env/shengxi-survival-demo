using System.Collections.Generic;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 生产配方：谁生产、每秒消耗什么、产出什么。
    /// 纯数据，配合 BuildingCatalog 保持「数值集中、改数不动代码」的纪律。
    /// </summary>
    public class RecipeDef
    {
        public BuildingType Producer;
        public int WoodPerSecond;
        public int StonePerSecond;
        public int FoodPerSecond;
        public ResourceType Output;
        public int OutputPerSecond;
    }

    /// <summary>
    /// 配方目录：目前只有工坊一条「木1 + 石1 → 建材1」。
    /// 后续加新产物/新生产者时只在这里追加，不动 ProductionSystem。
    /// </summary>
    public static class CraftingCatalog
    {
        public static readonly IReadOnlyList<RecipeDef> All = new List<RecipeDef>
        {
            new RecipeDef
            {
                Producer = BuildingType.Workshop,
                WoodPerSecond = 1,
                StonePerSecond = 1,
                Output = ResourceType.Material,
                OutputPerSecond = 1,
            },
        };

        private static readonly Dictionary<BuildingType, RecipeDef> ByProducer = BuildLookup();

        public static RecipeDef For(BuildingType producer) =>
            ByProducer.TryGetValue(producer, out var def) ? def : null;

        private static Dictionary<BuildingType, RecipeDef> BuildLookup()
        {
            var dict = new Dictionary<BuildingType, RecipeDef>();
            foreach (var recipe in All)
            {
                dict[recipe.Producer] = recipe;
            }

            return dict;
        }
    }
}
