using System;

namespace ShengXi.Simulation
{
    public enum ResourceType
    {
        Wood = 0,
        Stone = 1,
        Food = 2,
        Material = 3, // 建材：工坊合成，用于升级建筑
    }

    /// <summary>
    /// 全局共享资源池：木头 / 石头 / 食物 / 建材。
    /// 纯 C#，不依赖 Unity；加减资源都会通过 GameEvents 通知 UI。
    /// 仓库提升容量上限是 M2 的内容，这里先实现基础加减与上限约束。
    /// </summary>
    public class ResourcePool
    {
        /// <summary>默认容量上限；仓库建筑可在其上叠加。</summary>
        public const int DefaultCapacity = 100;

        private readonly int[] _amounts = new int[4];

        /// <summary>容量上限，默认 100；仓库建筑可提升（CapacityBonus）。</summary>
        public int Capacity { get; set; } = DefaultCapacity;

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

        /// <summary>
        /// 拆除仓库等建筑时降低容量上限；若当前资源超出新上限，按新上限截断并通知 UI。
        /// </summary>
        public void ReduceCapacity(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Capacity = System.Math.Max(DefaultCapacity, Capacity - amount);
            for (var i = 0; i < _amounts.Length; i++)
            {
                var before = _amounts[i];
                _amounts[i] = System.Math.Min(before, Capacity);
                if (_amounts[i] != before)
                {
                    GameEvents.RaiseResourceChanged((ResourceType)i, _amounts[i]);
                }
            }
        }

        /// <summary>读档恢复：直接设置数值，不发事件（由上层恢复完成后统一刷新）。</summary>
        public void Restore(int wood, int stone, int food, int capacity, int material = 0)
        {
            _amounts[(int)ResourceType.Wood] = Math.Max(0, wood);
            _amounts[(int)ResourceType.Stone] = Math.Max(0, stone);
            _amounts[(int)ResourceType.Food] = Math.Max(0, food);
            _amounts[(int)ResourceType.Material] = Math.Max(0, material);
            Capacity = capacity > 0 ? capacity : 100;
        }
    }
}
