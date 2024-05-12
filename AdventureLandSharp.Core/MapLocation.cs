using System.Numerics;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.Core;

public readonly record struct MapLocation(Map Map, Vector2 Position) : IComparable<MapLocation> {
    public override string ToString() => $"{Map.Name} {Position}";

    public int CompareTo(MapLocation other) {
        int mapComparison = Map.Name.CompareTo(other.Map.Name);
        if (mapComparison != 0) return mapComparison;

        int xComparison = Position.X.CompareTo(other.Position.X);
        if (xComparison != 0) return xComparison;

        return Position.Y.CompareTo(other.Position.Y);
    }

    public bool Equivalent(MapLocation other) => Map.Name == other.Map.Name && Position.Equivalent(other.Position);
    public bool Equivalent(MapLocation other, float epsilon) => Map.Name == other.Map.Name && Position.Equivalent(other.Position, epsilon);

    public MapGridCell Grid() => Position.Grid(Map);
    public Vector2 World() => Position;
    public MapGridCellData Data() => Position.Data(Map);
    public GameDataSmapCellData RpHash() => Position.RpHash(Map);
    public GameDataSmapCellData PHash() => Position.PHash(Map);

    public override int GetHashCode() => HashCode.Combine(Map.Name, Position.X, Position.Y);
}
