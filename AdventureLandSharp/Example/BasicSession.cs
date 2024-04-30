using System.Diagnostics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Interfaces;

namespace AdventureLandSharp.Example;

// Implements a basic session that will connect to the server and run a character.
// It will handle reconnecting if the connection is lost.
public class BasicSession(
    World world,
    ConnectionSettings settings,
    CharacterFactory characterFactory,
    bool withGui) : ISession
{
    public event Action<Socket, ICharacter>? OnInit;
    public event Action<Socket, ICharacter>? OnTick;
    public event Action<Socket, ICharacter>? OnFree;

    public ConnectionSettings Settings => _settings;

    public void EnterUpdateLoop() {
        while (!_disposed) {
            _socket = new(_world.Data, _settings);

            if (withGui) {
                _gui = new(_world, _socket);
            }

            while (!_disposed && !_socket.Connected) {
                _socket.Update();
                Thread.Yield();
            }

            try {   
                DoOneRun();
            } catch (Exception ex) {
                _log.Error($"DoOneRun update exception: {ex}");
            } finally {
                CleanUpAfterRun();
            }

            Thread.Sleep(TimeSpan.FromSeconds(5));
        }

        CleanUpAfterRun();
    }

    public void Dispose() {
        _disposed = true;
    }

    protected readonly World _world = world;
    protected readonly ConnectionSettings _settings = settings;
    protected readonly Logger _log = new(settings.Character.Name, "SESSION");
    protected Socket? _socket;
    protected BasicSessionGui? _gui;
    protected bool _disposed = false;

    private void DoOneRun() {
        Debug.Assert(_socket != null && _socket.Connected);

        CharacterClass cls = Enum.Parse<CharacterClass>(_settings.Character.Type, ignoreCase: true);
        ICharacter character = characterFactory(_world, _socket, cls);

        OnInit?.Invoke(_socket, character);

        while (!_disposed && _socket.Connected) {
            _socket.Update();

            if (_gui != null && !_gui.Update()) {
                return;
            }

            if (!character.Update()) {
                return;
            }

            OnTick?.Invoke(_socket, character);
            Thread.Sleep(1);
        }

        OnFree?.Invoke(_socket, character);
    }

    private void CleanUpAfterRun() {
        _socket?.Dispose();
        _socket = null;
        _gui?.Dispose();
        _gui = null;
    }
}
