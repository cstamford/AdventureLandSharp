using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.SecretSauce;

public interface ISessionEvent;

public class SessionEventBus() {
    public IReadOnlyList<string> Participants => _participants;

    public void RegisterParticipant(CharacterBase character) {
        _lock.Wait();
        _participants.Add(character.Entity.Name);
        _lock.Release();
    }

    public void UnregisterParticipant(CharacterBase character) {
        _lock.Wait();
        _participants.Remove(character.Entity.Name);
        _lock.Release();
    }

    public void Register<T>(CharacterBase character, Action<T> handler) where T: struct, ISessionEvent {
        _lock.Wait();
        _handlers[(character, typeof(T))] = handler;
        _lock.Release();
    }

    public void Unregister<T>(CharacterBase character) where T: struct, ISessionEvent {
        _lock.Wait();
        _handlers.Remove((character, typeof(T)));
        _lock.Release();
    }

    public void UnregisterAll(CharacterBase character) {
        _lock.Wait();

        foreach ((CharacterBase Receiver, Type EventType) key in _handlers.Keys
            .Where(x => x.Receiver == character))
        {
            _handlers.Remove(key);
        }

        _lock.Release();
    }

    public void Emit<T>(CharacterBase sender, T evt) where T: struct, ISessionEvent {
        _log.Debug([sender.Entity.Name, "EMIT"], evt.ToString()!);

        _lock.Wait();

        IEnumerable<Action<T>> handlers = _handlers
            .Where(x => x.Key.Receiver != sender && x.Key.EventType == typeof(T))
            .Select(x => x.Value)
            .Cast<Action<T>>();

        _lock.Release();

        foreach (Action<T> handler in handlers) {
            try {
                handler(evt);
            } catch (Exception e) {
                _log.Error($"Error handling event {evt}: {e}");
            }
        }
    }

    private readonly List<string> _participants = [];
    private readonly Dictionary<(CharacterBase Receiver, Type EventType), Delegate> _handlers = [];
    private readonly Logger _log = new("SessionEventBus");
    private readonly SemaphoreSlim _lock = new(1);
}

public sealed class SessionEventBusHandle : IDisposable {
    public CharacterBase Owner => _character;
    public IEnumerable<string> Participants => _bus.Participants.Where(x => x != _character.Entity.Name);

    public SessionEventBusHandle(SessionEventBus bus, CharacterBase character) {
        _bus = bus;
        _bus.RegisterParticipant(character);
        _character = character;
    }

    public void Dispose() {
        _bus.UnregisterAll(_character);
        _bus.UnregisterParticipant(_character);
    }

    public void Register<T>(Action<T> handler) where T: struct, ISessionEvent =>  _bus.Register(_character, handler);
    public void Unregister<T>() where T: struct, ISessionEvent => _bus.Unregister<T>(_character);
    public void Emit<T>(T evt) where T: struct, ISessionEvent => _bus.Emit(_character, evt);

    private readonly CharacterBase _character;
    private readonly SessionEventBus _bus;
}
