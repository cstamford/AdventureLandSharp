using AdventureLandSharp.Utility;

namespace AdventureLandSharp.Helpers;

public class Cooldown(TimeSpan cd, float cdMulti = 1) {
    public TimeSpan Duration {
        get => cd * cdMulti;
        set => cd = value;
    }
    public bool Ready => DateTimeOffset.UtcNow >= _end;
    public void Restart() => _end = DateTimeOffset.UtcNow.Add(Duration);
    private DateTimeOffset _end = DateTimeOffset.UtcNow;
}

public static class CooldownBtExtensions {
    public static Status Then(this Cooldown cd, Func<Status> action) {
        if (cd.Ready) {
            Status status = action();

            if (status == Status.Success) {
                cd.Restart();
            }

            return status;
        }

        return Status.Fail;
    }
    public static Status Then(this Cooldown cd, INode node) => cd.Then(node.Tick);

    public static Do ThenDo(this Cooldown cd, Func<Status> action) => new(() => cd.Then(action));
    public static Do ThenDo(this Cooldown cd, INode node) => new(() => cd.Then(node));

    public static If IfThenDo(this Cooldown cd, Func<bool> condition, Func<Status> action) => new(condition, cd.ThenDo(action));
    public static If IfThenDo(this Cooldown cd, Func<bool> condition, INode node) => new(condition, cd.ThenDo(node));
}