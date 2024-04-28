namespace AdventureLandSharp.Core.Util;

public static class LinqExtensions {
    public static bool TryFirst<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out TSource? result) {
        foreach (TSource item in source) {
            if (predicate(item)) {
                result = item;
                return true;
            }
        }

        result = default;
        return false;
    }
}
