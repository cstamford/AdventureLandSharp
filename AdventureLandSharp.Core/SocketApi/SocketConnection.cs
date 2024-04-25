using AdventureLandSharp.Core.HttpApi;
using AdventureLandSharp.Core.Util;
using SocketIOClient;
using System.Diagnostics;
using System.Text.Json;

namespace AdventureLandSharp.Core.SocketApi;

public readonly record struct ConnectionSettings(
    string UserId,
    string AuthToken,
    ApiServer Server,
    ApiCharacter Character);

public class Connection(ConnectionSettings settings) : IDisposable {
    public event Action<JsonElement>? OnConnected;
    public event Action? OnDisconnected;

    public bool Connected => _connected && _authenticated && _ready;
    public ConnectionSettings Settings => settings;
    public SocketIOClient.SocketIO? SocketIo => _socketIo;

    public void Update() {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (!_connected) {
            CloseExistingConnection();

            if (now > _reconnectTimeout) {
                StartConnection();
                _authTimeout = now.AddSeconds(10);
                _reconnectTimeout = now.AddSeconds(15);
            }
        }

        if (!_authenticated || !_ready) {
            if (now > _authTimeout) {
                CloseExistingConnection();
            }

            return;
        }
    }

    public void Dispose() {
        CloseExistingConnection();
    }

    private SocketIOClient.SocketIO? _socketIo;
    private DateTimeOffset _authTimeout = DateTimeOffset.UtcNow;
    private DateTimeOffset _reconnectTimeout = DateTimeOffset.UtcNow;

    private bool _connected;
    private bool _authenticated;
    private bool _ready;

    private void StartConnection() {
        _socketIo = new($"http://{settings.Server.Addr}:{settings.Server.Port}");

        SafeSocketOn("welcome", async _ => {
            Log.Info($"[{settings.Character.Name} CONN] Welcome message received. Responding with loaded message.");
            await _socketIo.EmitAsync("loaded", new Outbound.Loaded(
                Success: true,
                Width: 1920,
                Height: 1080,
                Scale: 2));
        });

        SafeSocketOn("entities", async e => {
            if (!_authenticated) {
                Log.Info($"[{settings.Character.Name} CONN] Initial entities message received. Responding with auth message.");

                await _socketIo.EmitAsync("auth", new Outbound.Auth(
                    AuthToken: settings.AuthToken,
                    CharacterId: settings.Character.Id,
                    UserId: settings.UserId,
                    Width: 1920,
                    Height: 1080,
                    Scale: 2,
                    NoHtml: false,
                    NoGraphics: false
                ));

                _authenticated = true;
            }
        });

        SafeSocketOn("start", e => {
            Log.Info($"[{settings.Character.Name} CONN] Start message received.");
            OnConnected?.Invoke(e.GetValue<JsonElement>());
            _ready = true;
        });

        _socketIo.On("disconnect_reason", e => HandleError("disconnect_reason", e));
        _socketIo.OnDisconnected += (_, e) => HandleError("_socketIo.OnDisconnected", e);
        _socketIo.OnError += (_, e) => HandleError("_socketIo.OnError", e);

        _socketIo.ConnectAsync();
        _connected = true;
    }

    private void HandleError(string type, object e) {
        Log.Error($"{type}: {e}");
        CloseExistingConnection();
    }

    private void SafeSocketOn(string name, Action<SocketIOResponse> cb) {
        Debug.Assert(_socketIo != null);
    
        _socketIo.On(name, e => {
            try {
                cb(e);
            } catch {
                HandleError(name, e);
            }
        });
    }

    private void CloseExistingConnection() {
        if (_ready) {
            OnDisconnected?.Invoke();
            _ready = false;
        }

        try {
            _socketIo?.DisconnectAsync();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            _socketIo?.Dispose();
        } catch (Exception e) {
            Log.Error($"Error disposing _socketIo: {e}");
        } finally {
            _socketIo = null;
        }

        _authenticated = false;
        _connected = false;
    }
}
