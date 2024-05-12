namespace AdventureLandSharp.Core;

public enum MapGridHeuristic {
    Manhattan,
    Euclidean,
    Diagonal
}

public readonly record struct MapGridCell(ushort X, ushort Y) {
    public MapGridCell(int x, int y) : this((ushort)x, (ushort)y) 
    { }

    public MapGridCell(MapGridCell original, int offsetX, int offsetY) : this(
        (ushort)(original.X + offsetX),
        (ushort)(original.Y + offsetY)) 
    { }

    public float HeuristicCost(MapGridCell other, MapGridHeuristic heuristic) => heuristic switch {
        MapGridHeuristic.Manhattan => ManhattanDistance(this, other),
        MapGridHeuristic.Euclidean => EuclideanDistance(this, other),
        MapGridHeuristic.Diagonal => DiagonalDistance(this, other),
        _ => throw new ArgumentOutOfRangeException(nameof(heuristic))
    };

    public static float ManhattanDistance(MapGridCell lhs, MapGridCell rhs) {
        return MathF.Abs(lhs.X - rhs.X) + MathF.Abs(lhs.Y - rhs.Y);
    }

    public static float EuclideanDistance(MapGridCell lhs, MapGridCell rhs) {
        float dx = lhs.X - rhs.X;
        float dy = lhs.Y - rhs.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public static float DiagonalDistance(MapGridCell lhs, MapGridCell rhs) {
        float dmax = MathF.Max(MathF.Abs(lhs.X - rhs.X), MathF.Abs(lhs.Y - rhs.Y));
        float dmin = MathF.Min(MathF.Abs(lhs.X - rhs.X), MathF.Abs(lhs.Y - rhs.Y));
        return 1.4142136f * dmin + (dmax - dmin);
    }

    public override int GetHashCode() => X | (Y << 16);
}

public readonly record struct MapGridCellData(
    float Cost,
    float CornerScore,
    int RpHashScore,
    int PHashScore)
{
    public readonly bool IsWalkable => Cost >= 1 && RpHashScore < GameDataSmapCellData.JailValue;

    public static MapGridCellData Walkable => new(
        Cost: 1,
        CornerScore: 0,
        RpHashScore: 0,
        PHashScore: 0
    );

    public static MapGridCellData Unwalkable => new(
        Cost: 0,
        CornerScore: 0,
        RpHashScore: GameDataSmapCellData.MaxValue,
        PHashScore: GameDataSmapCellData.MaxValue
    );
}
