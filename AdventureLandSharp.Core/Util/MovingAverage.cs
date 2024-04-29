using System.Numerics;

namespace AdventureLandSharp.Core.Util;

public class MovingAverage<TValue>(TimeSpan window) where TValue: INumber<TValue> {
    public TValue Min => _values.MinBy(x => x.Data).Data;
    public TValue Sum => _values.Select(x => x.Data).Aggregate((acc, x) => acc + x);
    public TValue Max => _values.MaxBy(x => x.Data).Data;

    public int Samples => _values.Count;

    public void Add(TValue value) {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        _values.AddLast((now, value));

        while (_values.Count > 0 && now > _values.First!.Value.Time.Add(window)) {
            _values.RemoveFirst();
        }
    }

     private readonly LinkedList<(DateTimeOffset Time, TValue Data)> _values = [];
}
