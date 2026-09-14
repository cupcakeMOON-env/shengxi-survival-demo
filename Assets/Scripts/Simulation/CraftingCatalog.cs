using System.Collections.Generic;

namespace ShengXi.Simulation
{
    /// <summary>配方的一种原料：每秒消耗多少。</summary>
    public class RecipeIngredient
    {
        public ResourceType Type;
        public int AmountPerSecond;
    }

    /// <summary>
    /// 生产配方：谁生产、每秒消耗哪些原料、产出什么。
    /// 纯数据；同一生产者可以有多条配方，ProductionSystem 会并行结算
    /// （原料够、产物没占满容量就各自产），要加配方只改这里。
    /// </summary>
    public class RecipeDef
    {
        public string Name;
        public BuildingType Producer;
        public RecipeIngredient[] InputsPerSecond;
        public ResourceType Output;
        public int OutputPerSecond;
    }

    /// <summary>
    /// 配方目录：工坊目前两条并行产线
    /// 「木1 + 石1 → 建材1」「木1 + 食物1 → 修理包1」。
    /// 后续加新产物/新生产者时只在这里追加，不动 ProductionSystem。
    /// </summary>
    public static class CraftingCatalog
    {
        public static readonly IReadOnlyList<RecipeDef> All = new List<RecipeDef>
        {
            new RecipeDef
            {
                Name = "建材",
                Producer = BuildingType.Workshop,
                InputsPerSecond = new[]
                {
                    Ingredient(ResourceType.Wood, 1),
                    Ingredient(ResourceType.Stone, 1),
                },
                Output = ResourceType.Material,
                OutputPerSecond = 1,
            },
            new RecipeDef
            {
                Name = "修理包",
                Producer = BuildingType.Workshop,
                InputsPerSecond = new[]
                {
                    Ingredient(ResourceType.Wood, 1),
                    Ingredient(ResourceType.Food, 1),
                },
                Output = ResourceType.RepairKit,
                OutputPerSecond = 1,
            },
        };

        private static readonly Dictionary<BuildingType, IReadOnlyList<RecipeDef>> ByProducer = BuildLookup();

        /// <summary>某个生产者的全部配方；没有配方时返回空列表（不是 null）。</summary>
        public static IReadOnlyList<RecipeDef> For(BuildingType producer) =>
            ByProducer.TryGetValue(producer, out var list) ? list : System.Array.Empty<RecipeDef>();

        private static RecipeIngredient Ingredient(ResourceType type, int amountPerSecond) => new RecipeIngredient
        {
            Type = type,
            AmountPerSecond = amountPerSecond,
        };

        private static Dictionary<BuildingType, IReadOnlyList<RecipeDef>> BuildLookup()
        {
            var grouped = new Dictionary<BuildingType, List<RecipeDef>>();
            foreach (var recipe in All)
            {
                if (!grouped.TryGetValue(recipe.Producer, out var list))
                {
                    list = new List<RecipeDef>();
                    grouped[recipe.Producer] = list;
                }

                list.Add(recipe);
            }

            var lookup = new Dictionary<BuildingType, IReadOnlyList<RecipeDef>>();
            foreach (var pair in grouped)
            {
                lookup[pair.Key] = pair.Value;
            }

            return lookup;
        }
    }
}
