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

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static float Dist(Vector2 a, Vector2 b, Vector2 aSize, Vector2 bSize) {
        // Source: https://github.com/kaansoral/adventureland/blob/585169d3927e15c33a1d55bce2d75b0eb5aea226/js/common_functions.js#L646C10-L646C18
        // Mostly copy/paste to ensure the same behavior.

        float a_x = a.X;
        float a_y = a.Y;
        float b_x = b.X;
        float b_y = b.Y;

        float a_w2 = aSize.X / 2;
        float a_h = aSize.Y;
        float b_w2 = bSize.X / 2;
        float b_h = bSize.Y;

        if ((a_x - a_w2) <= (b_x + b_w2) &&
            (a_x + a_w2) >= (b_x - b_w2) &&
            (a_y) >= (b_y - b_h) &&
            (a_y - a_h) <= (b_y))
        {
            return 0;
        }

        float min = float.MaxValue;

        Span<Vector2> aCorners = [
            new(a_x + a_w2, a_y - a_h),
            new(a_x + a_w2, a_y),
            new(a_x - a_w2, a_y - a_h),
            new(a_x - a_w2, a_y)
        ];

        Span<Vector2> bCorners = [
            new(b_x + b_w2, b_y - b_h),
            new(b_x + b_w2, b_y),
            new(b_x - b_w2, b_y - b_h),
            new(b_x - b_w2, b_y)
        ];

        foreach (Vector2 a_c in aCorners) {
            foreach (Vector2 b_c in bCorners) {
                float d = Vector2.Distance(a_c, b_c);
                if (d < min) {
                    min = d;
                }
            }
        }

        return min;
    }
}
