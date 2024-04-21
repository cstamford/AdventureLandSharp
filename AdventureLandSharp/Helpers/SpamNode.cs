using AdventureLandSharp.Utility;

namespace AdventureLandSharp.Helpers;

// TODO: With game_response, we will instead spam until the Task resolves to success.
public class SpamNode(Func<Status> spam, Cooldown cd, Action fnRestartCd, int spamAmount = SpamNode.DefaultSpamAmount) : INode {
    public const int DefaultSpamAmount = 5;

    public SpamNode(INode node, Cooldown cd, Action fnRestartCd, int spamAmount = DefaultSpamAmount)
        : this(node.Tick, cd, fnRestartCd, spamAmount)
    { }

    public SpamNode(Action spam, Cooldown cd, Action fnRestartCd, int spamAmount = DefaultSpamAmount)
        : this(new Do(() => spam()), cd, fnRestartCd, spamAmount)
    { }

    public Status Tick() {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // First time we've spammed - trigger the ability once, then fall into spam pattern.
        if (_spamLeft == _spamAmountFull) {
            if (!cd.Ready || spam() == Status.Fail) {
                return Status.Fail;
            }

            --_spamLeft;
            _nextSpam = now.Add(SpamInterval);
            fnRestartCd();

            return Status.Running;
        }

        // Every subsequent spam after the first, until reset.
        if (now >= _nextSpam) {
            Status status = spam();

            if (status == Status.Fail || --_spamLeft == 0) {
                _spamLeft = _spamAmountFull;
                return status;
            } 

            _nextSpam = _nextSpam.Add(SpamInterval);
        }

        return Status.Running;
    }

    private static readonly TimeSpan JitterBuffer = TimeSpan.FromMilliseconds(100);

    private int _spamLeft = spamAmount;
    private readonly int _spamAmountFull = spamAmount;
    private DateTimeOffset _nextSpam;

    private TimeSpan SpamInterval => JitterBuffer.Divide(_spamAmountFull);
}
