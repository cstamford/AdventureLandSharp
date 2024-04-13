using System.Diagnostics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.Game;

public class Session(
    World world,
    ConnectionSettings settings,
    ICharacterFactory characterFactory,
    bool withGui) : IDisposable
{
    public void EnterUpdateLoop() {
        while (!_disposed) {
            _socket = new(_world.Data, _settings, OnSend, OnRecv);

            if (withGui) {
                _gui = new(_world, _socket);
            }

            while (!_socket.Connected) {
                _socket.Update();
                Thread.Yield();
            }

            try {   
                DoOneRun();
            } catch (Exception ex) {
                Log.Error($"[{_settings.Character.Name}] Exception: {ex}");
            } finally {
                CleanUpAfterRun();
            }

            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }

    public void Dispose() {
        CleanUpAfterRun();
        _disposed = true;
    }

    private readonly World _world = world;
    private readonly ConnectionSettings _settings = settings;
    private Socket? _socket;
    private GameGui? _gui;
    private bool _disposed = false;

    private void OnRecv(string evt, object data) {
        Log.Debug($"[{_settings.Character.Name} RECV] Event: {evt}, Data: {data}");
    }

    private void OnSend(string evt, object data) {
        Log.Debug($"[{_settings.Character.Name} SEND] Event: {evt}, Data: {data}");
    }

    private void DoOneRun() {
        Debug.Assert(_socket != null && _socket.Connected);

        ICharacter character = CreateCharacterForRun();
        DateTimeOffset lastTick = DateTimeOffset.UtcNow;

        while (_socket.Connected) {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            float dt = (float)now.Subtract(lastTick).TotalSeconds;
            lastTick = now;

            _socket.Update();

            if (_gui != null && !_gui.Update()) {
                return;
            }

            if (!character.Update(dt)) {
                return;
            }

            Thread.Yield();
        }
    }

    private void CleanUpAfterRun() {
        _socket?.Dispose();
        _socket = null;
        _gui?.Dispose();
        _gui = null;
    }

    private ICharacter CreateCharacterForRun() {
        CharacterClass cls = _settings.Character.Type switch {
            "mage" => CharacterClass.Mage,
            "merchant" => CharacterClass.Merchant,
            "paladin" => CharacterClass.Paladin,
            "priest" => CharacterClass.Priest,
            "ranger" => CharacterClass.Ranger,
            "rogue" => CharacterClass.Rogue,
            "warrior" => CharacterClass.Warrior,
            _ => throw new()
        };

        return characterFactory.Create(cls, _world, _socket!);
    }
}

