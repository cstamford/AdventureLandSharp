using System.Numerics;
using System.Runtime.CompilerServices;
using AdventureLandSharp.Core.SocketApi;

namespace AdventureLandSharp.Core.Util;

public static class EntityExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dist(this Entity a, Entity b) => Collision.Dist(a.Position, b.Position, a.Size, b.Size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dist(this Entity a, Vector2 b) => Collision.Dist(a.Position, b, a.Size, Vector2.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dist(this Vector2 a, Entity b) => Collision.Dist(a, b.Position, Vector2.Zero, b.Size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dist(this Entity a, Vector2 b, Vector2 bSize) => Collision.Dist(a.Position, b, a.Size, bSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SimpleDist(this Vector2 a, Vector2 b) =>  Vector2.Distance(a, b);
}
