using AdventureLandSharp.Core.HttpApi;
using AdventureLandSharp.Core.Util;
using System.Text.Json;

namespace AdventureLandSharp.Core.SocketApi;

public readonly record struct ConnectionSettings(
    string UserId,
    string AuthToken,
    ApiServer Server,
    ApiCharacter Character);

public class Connection(ConnectionSettings settings) : IDisposable {
    public event Action<Dictionary<string, JsonElement>>? OnConnected;
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
        // Socket login flow.
        // 1. Wait for "welcome" from server.
        // 2. Emit "loaded".
        // 3. Wait for "entities" from server.
        // 4. Emit "auth".
        // 5. Wait for "start" from server.
        _socketIo = new($"http://{settings.Server.Addr}:{settings.Server.Port}");

        // Note: welcome will give us server and map info, but this is for the spectator.
        // It has nothing to do with our characters or location, so we just ignore it.
        _socketIo.On("welcome", async _ => {
            await _socketIo.EmitAsync("loaded", new Outbound.Loaded(
                Success: true,
                Width: 1920,
                Height: 1080,
                Scale: 2));
        });

        // Basically contains a snapshot of the game state.
        _socketIo.On("entities", async e => {
            if (!_authenticated) {
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

        // Identical to player, but called once at the very start.
        _socketIo.On("start", e => {
            OnConnected?.Invoke(e.GetValue<Dictionary<string, JsonElement>>());
            _ready = true;
        });

        // Kick off the connection process.
        _socketIo.ConnectAsync();
        _connected = true;
    }

    private void CloseExistingConnection() {
        if (_ready) {
            OnDisconnected?.Invoke();
            _ready = false;
        }

        if (_socketIo != null) {
            _socketIo.DisconnectAsync();
            Thread.Sleep(200); // forgive me

            try {
                _socketIo.Dispose();
            } catch (Exception e) {
                Log.Error($"Error disposing _socketIo: {e}");
            } finally {
                _socketIo = null;
            }
        }

        _authenticated = false;
        _connected = false;
    }
}