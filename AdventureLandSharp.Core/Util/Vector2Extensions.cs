using System.Numerics;

namespace AdventureLandSharp.Core.Util;

public static class Vector2Extensions {
    public static float SimpleDist(this Vector2 a, Vector2 b) =>  Vector2.Distance(a, b);
    public static bool Equivalent(this Vector2 a, Vector2 b) => a.Equivalent(b, MapGridTerrain.Epsilon);
    public static bool Equivalent(this Vector2 a, Vector2 b, float epsilon) => a.SimpleDist(b) <= epsilon;
}
