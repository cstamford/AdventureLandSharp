using System.Numerics;
using System.Runtime.CompilerServices;

namespace AdventureLandSharp.Core;

public static class Collision {
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static float LineVsCircle(Vector2 from, Vector2 to, Vector2 circleCenter) {
        Vector2 AB = to - from;
        Vector2 AC = circleCenter - from;

        // Project AC onto AB to find the parameterized position t of the closest point
        float t = Vector2.Dot(AC, AB) / Vector2.Dot(AB, AB);

        // Find the closest point on the line
        Vector2 P;

        if (t < 0.0f) {
            P = from; // Closest to A
        } else if (t > 1.0f) {
            P = to; // Closest to B
        } else {
            P = from + t * AB; // Closest point on the segment
        }

        // Distance from C to P
        return Vector2.Distance(circleCenter, P);
    }
}