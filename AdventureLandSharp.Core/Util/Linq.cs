namespace AdventureLandSharp.Core.Util;

public static class LinqExtensions {
    public static T? FirstOrNull<T>(this IEnumerable<T> source) where T : struct {
        return source.Any() ? source.First() : null;
    }

    public static T? FirstOrNull<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : struct {
        foreach (T item in source) {
            if (predicate(item)) {
                return item;
            }
        }
        return null;
    }
}
