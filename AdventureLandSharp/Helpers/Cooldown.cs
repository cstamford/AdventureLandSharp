namespace AdventureLandSharp.Helpers;

public class Cooldown(TimeSpan cd) {
    public TimeSpan Duration { get; set; } = cd;
    public TimeSpan Remaining => _start.Add(Duration).Subtract(DateTimeOffset.UtcNow);
    public bool Ready => Remaining <= TimeSpan.Zero;
    
    public void Restart() => _start = DateTimeOffset.UtcNow;
    public void Restart(TimeSpan duration) {
        Duration = duration;
        _start = DateTimeOffset.UtcNow;
    }

    private DateTimeOffset _start = DateTimeOffset.MinValue;
}
