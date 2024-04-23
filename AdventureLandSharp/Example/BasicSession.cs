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

    public ConnectionSettings Settings => _settings;

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

        CleanUpAfterRun();
    }

    public void Dispose() {
        _disposed = true;
    }

    private readonly World _world = world;
    private readonly ConnectionSettings _settings = settings;
    private Socket? _socket;
    private BasicSessionGui? _gui;
    private bool _disposed = false;

    private void OnRecv(string evt, object data) {
        Log.Debug($"[{_settings.Character.Name} RECV] Event: {evt}, Data: {data}");
    }

    private void OnSend(string evt, object data) {
        Log.Debug($"[{_settings.Character.Name} SEND] Event: {evt}, Data: {data}");
    }

    private void DoOneRun() {
        Debug.Assert(_socket != null && _socket.Connected);

        ICharacter character =  characterFactory(_world, _socket!, _settings.Character.Type switch {
            "mage" => CharacterClass.Mage,
            "merchant" => CharacterClass.Merchant,
            "paladin" => CharacterClass.Paladin,
            "priest" => CharacterClass.Priest,
            "ranger" => CharacterClass.Ranger,
            "rogue" => CharacterClass.Rogue,
            "warrior" => CharacterClass.Warrior,
            _ => throw new()
        });

        OnInit?.Invoke(_socket, character);

        while (_socket.Connected) {
            _socket.Update();

            if (_gui != null && !_gui.Update()) {
                return;
            }

            if (!character.Update()) {
                return;
            }

            OnTick?.Invoke(_socket, character);
            Thread.Yield();
        }
    }

    private void CleanUpAfterRun() {
        _socket?.Dispose();
        _socket = null;
        _gui?.Dispose();
        _gui = null;
    }
}
