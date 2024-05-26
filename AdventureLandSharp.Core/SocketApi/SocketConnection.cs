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
    public event Action? OnCreateSocket;
    public event Action<JsonElement>? OnConnected;
    public event Action? OnDisconnected;

    public bool Connected => _connected && _authenticated && _ready;
    public ConnectionSettings Settings => settings;

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

    public Task EmitAsync(string name, object data) {
        Debug.Assert(_socketIo != null);

        return _socketIo.EmitAsync(name, data);
    }

    public void On(string name, Action<SocketIOResponse> cb) {
        Debug.Assert(_socketIo != null);

        if (!_handlers.TryGetValue(name, out List<Action<SocketIOResponse>>? handlers)) {
            _socketIo.On(name, e => {
                foreach (Action<SocketIOResponse> handler in _handlers[name]) {
                    try {
                        handler(e);
                    } catch (Exception ex) {
                        HandleError(name, e, ex);
                    }
                }
            });

            handlers = [];
            _handlers.Add(name, handlers);
        }

        handlers.Add(cb);
    }

    public void OnAny(Action<string, SocketIOResponse> cb) {
        Debug.Assert(_socketIo != null);

        _socketIo.OnAny((name, e) => {
            try {
                cb(name, e);
            } catch (Exception ex) {
                HandleError(name, e, ex);
            }
        });
    }

    private readonly Logger _log = new(settings.Character.Name, "Connection");

    private SocketIOClient.SocketIO? _socketIo;
    private DateTimeOffset _authTimeout = DateTimeOffset.UtcNow;
    private DateTimeOffset _reconnectTimeout = DateTimeOffset.UtcNow;
    private Dictionary<string, List<Action<SocketIOResponse>>> _handlers = [];

    private bool _connected;
    private bool _authenticated;
    private bool _ready;

    private void StartConnection() {
        _socketIo = new($"http://{settings.Server.Addr}:{settings.Server.Port}");

        try {
            OnCreateSocket?.Invoke();
        } catch (Exception e) {
            HandleError("(user) OnCreateSocket", e);
        }

        On("welcome", _ => {
            _log.Info($"Welcome message received. Responding with loaded message.");
            _socketIo.EmitAsync("loaded", new Outbound.Loaded(
                Success: true,
                Width: 1920,
                Height: 1080,
                Scale: 2));
        });

        On("entities", e => {
            if (!_authenticated) {
                _authenticated = true;

                _log.Info($"Initial entities message received. Responding with auth message.");
                _socketIo.EmitAsync("auth", new Outbound.Auth(
                    AuthToken: settings.AuthToken,
                    CharacterId: settings.Character.Id,
                    UserId: settings.UserId,
                    Width: 1920,
                    Height: 1080,
                    Scale: 2,
                    NoHtml: false,
                    NoGraphics: false
                ));
            }
        });

        On("start", e => {
            _log.Info($"Start message received.");
            OnConnected?.Invoke(e.GetValue<JsonElement>());
            _ready = true;
        });

        _socketIo.OnDisconnected += (_, e) => HandleError("_socketIo.OnDisconnected", e);
        _socketIo.OnError += (_, e) => HandleError("_socketIo.OnError", e);

        _socketIo.ConnectAsync();
        _connected = true;
    }

    private void HandleError(string type, object e) {
        _log.Error($"{type}: {e}");
        CloseExistingConnection();
    }

    private void HandleError(string type, object e, Exception ex) {
        _log.Error($"{type}: {e} with exception: {ex}");
        CloseExistingConnection();
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
            _log.Error($"Error disposing _socketIo: {e}");
        } finally {
            _socketIo = null;
        }

        _authenticated = false;
        _connected = false;
        _handlers.Clear();
    }
}
