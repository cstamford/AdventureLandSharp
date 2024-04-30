namespace AdventureLandSharp.Core.Util;

public static class LinqExtensions {
    public static T? FirstOrNull<T>(this IEnumerable<T> source) where T : struct {
        return source.Any() ? source.First() : null;
    }
}
