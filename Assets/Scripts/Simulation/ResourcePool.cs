using System;

namespace ShengXi.Simulation
{
    public enum ResourceType
    {
        Wood = 0,
        Stone = 1,
        Food = 2,
    }

    /// <summary>
    /// 全局共享资源池：木头 / 石头 / 食物。
    /// 纯 C#，不依赖 Unity；加减资源都会通过 GameEvents 通知 UI。
    /// 仓库提升容量上限是 M2 的内容，这里先实现基础加减与上限约束。
    /// </summary>
    public class ResourcePool
    {
        private readonly int[] _amounts = new int[3];

        /// <summary>容量上限，默认 100；仓库建筑可提升（CapacityBonus）。</summary>
        public int Capacity { get; set; } = 100;

        public int GetAmount(ResourceType type) => _amounts[(int)type];

        public void Add(ResourceType type, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _amounts[(int)type] = Math.Min(_amounts[(int)type] + amount, Capacity);
            GameEvents.RaiseResourceChanged(type, _amounts[(int)type]);
        }

        public bool CanSpend(ResourceType type, int amount) => amount >= 0 && amount <= _amounts[(int)type];

        public bool TrySpend(ResourceType type, int amount)
        {
            if (!CanSpend(type, amount))
            {
                return false;
            }

            _amounts[(int)type] -= amount;
            GameEvents.RaiseResourceChanged(type, _amounts[(int)type]);
            return true;
        }

        /// <summary>读档恢复：直接设置数值，不发事件（由上层恢复完成后统一刷新）。</summary>
        public void Restore(int wood, int stone, int food, int capacity)
        {
            _amounts[(int)ResourceType.Wood] = Math.Max(0, wood);
            _amounts[(int)ResourceType.Stone] = Math.Max(0, stone);
            _amounts[(int)ResourceType.Food] = Math.Max(0, food);
            Capacity = capacity > 0 ? capacity : 100;
        }
    }
}
