using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Helpers;
using AdventureLandSharp.Utility;

namespace AdventureLandSharp.SecretSauce.Character;

public abstract partial class CharacterBase {
    protected virtual INode ActionBuild() => new If(
        () => AttackTarget.HasValue,
        Skill("attack", () => Socket.Emit<Outbound.Attack>(new(AttackTarget!.Value.Id)))
    );

    protected virtual void ActionUpdate() => _actionBt.Tick();

    protected JitterSpamNode Skill(string name, Action action) => new(Cooldown(name), action);
    protected JitterSpamNode Skill(string name, TimeSpan cooldown, Action action) => new(Cooldown(name, cooldown), action);

    protected Cooldown Cooldown(string name) {
        if (_cooldowns.TryGetValue(name, out Cooldown? ability)) {
            return ability;
        }

        ability = new Cooldown(TimeSpan.FromMilliseconds(100));
        _cooldowns.Add(name, ability);

        return ability;
    }

    protected Cooldown Cooldown(string name, TimeSpan cooldown) {
        if (_cooldowns.TryGetValue(name, out Cooldown? ability)) {
            ability.Duration = cooldown;
            return ability;
        }

        ability = new Cooldown(cooldown);
        _cooldowns.Add(name, ability);

        return ability;
    }

    private readonly INode _actionBt;
    private Dictionary<string, Cooldown> _cooldowns = [];
}

public class JitterSpamNode(Cooldown cooldown, Action action) : INode {
    public Status Tick() {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (cooldown.Remaining >= _spamDuration) {
            return Status.Fail;
        }

        if (now >= _spamTime) {
            action();
            _spamTime = now.Add(_spamInterval);
            return Status.Success;
        }

        return Status.Running;
    }

    private DateTimeOffset _spamTime;
    private static readonly TimeSpan _spamInterval = TimeSpan.FromMilliseconds(1);
    private static readonly TimeSpan _spamDuration = TimeSpan.FromMilliseconds(5);
}
