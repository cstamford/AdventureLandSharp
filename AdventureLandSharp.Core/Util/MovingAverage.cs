using System.Numerics;

namespace AdventureLandSharp.Core.Util;

public class MovingAverage<TValue>(TimeSpan window) where TValue: struct, INumber<TValue> {
    public TValue Min => _values.Count == 0 ? default : _values.MinBy(x => x.Data).Data;
    public TValue Sum => _values.Count == 0 ? default : _values.Select(x => x.Data).Aggregate((acc, x) => acc + x);
    public TValue Max => _values.Count == 0 ? default : _values.MaxBy(x => x.Data).Data;

    public int Samples => _values.Count;
    public TimeSpan Window => window;

    public MovingAverage(MovingAverage<TValue> other) : this(other.Window) {
        _values = new(other._values);
    }

    public void Add(TValue value) => Add(DateTimeOffset.UtcNow, value);

    public void Add(DateTimeOffset now, TValue value) {
        _values.AddLast((now, value));
        while (_values.Count > 0 && now > _values.First!.Value.Time.Add(window)) {
            _values.RemoveFirst();
        }
    }

    public void Clear() => _values.Clear();

    private readonly LinkedList<(DateTimeOffset Time, TValue Data)> _values = [];
}
