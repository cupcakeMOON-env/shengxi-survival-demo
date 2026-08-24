namespace ShengXi.Simulation
{
    /// <summary>
    /// 纯 C# 的网格坐标。
    /// 刻意不用 UnityEngine.Vector2Int，保证模拟层可以脱离 Unity 编译、测试与运行。
    /// </summary>
    public readonly struct GridPos
    {
        public int X { get; }
        public int Y { get; }

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridPos other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is GridPos other && Equals(other);

        public override int GetHashCode() => unchecked((X * 397) ^ Y);

        public static bool operator ==(GridPos a, GridPos b) => a.Equals(b);

        public static bool operator !=(GridPos a, GridPos b) => !a.Equals(b);

        public override string ToString() => $"({X}, {Y})";
    }
}
