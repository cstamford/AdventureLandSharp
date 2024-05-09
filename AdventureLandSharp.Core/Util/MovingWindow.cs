namespace AdventureLandSharp.Core.Util;

public class MovingWindow<TValue>(TimeSpan window) {
    public IEnumerable<(DateTimeOffset Time, TValue Data)> Values => _values;

    public void Add(TValue value) {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        _values.AddLast((now, value));

        while (_values.Count > 0 && now > _values.First!.Value.Time.Add(window)) {
            _values.RemoveFirst();
        }
    }

    private readonly LinkedList<(DateTimeOffset Time, TValue Data)> _values = [];
}
