namespace AdventureLandSharp.Core.SocketApi;

public class ConnectionSettings(
    string userId,
    string authToken,
    ApiServer server,
    ApiCharacter character)
{
    public string UserId { get; } = userId;
    public string AuthToken { get; } = authToken;
    public ApiServer Server { get; } = server;
    public ApiCharacter Character { get; } = character;
}

public class SocketConnection : IDisposable
{
    private readonly SemaphoreSlim _entitiesSemaphore = new(0);
    private readonly ILogger _logger;
    private readonly ConnectionSettings _settings;
    private readonly SemaphoreSlim _startSemaphore = new(0);

    private readonly SemaphoreSlim _welcomeSemaphore = new(0);

    private bool _authenticated;
    private DateTimeOffset _authTimeout = DateTimeOffset.UtcNow;

    private bool _connected;
    private bool _ready;
    private DateTimeOffset _reconnectTimeout = DateTimeOffset.UtcNow;

    public SocketConnection(ILogger<SocketConnection> logger, ConnectionSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public bool Connected => _connected && _authenticated && _ready;
    public SocketIOClient.SocketIO? SocketIo { get; private set; }

    public void Dispose()
    {
        // Dispose can't really be async, so we will force the close to be sync here.
        CloseExistingConnection().GetAwaiter().GetResult();
        _welcomeSemaphore.Dispose();
        _entitiesSemaphore.Dispose();
        _startSemaphore.Dispose();

        GC.SuppressFinalize(this);
    }

    public event Action<JsonElement>? OnConnected;
    public event Action? OnDisconnected;

    public async Task Update()
    {
        var now = DateTimeOffset.UtcNow;

        if (!_connected)
        {
            await CloseExistingConnection();

            if (now > _reconnectTimeout)
            {
                await Connect();
                _authTimeout = now.AddSeconds(10);
                _reconnectTimeout = now.AddSeconds(15);
            }
        }

        if (_authenticated && _ready) return;

        if (now > _authTimeout) await CloseExistingConnection();
    }

    private async Task Connect()
    {
        // Socket login flow.
        // 1. Wait for "welcome" from server.
        // 2. Emit "loaded".
        // 3. Wait for "entities" from server.
        // 4. Emit "auth".
        // 5. Wait for "start" from server.

        // Need a way to get the protocol here, technically the server should really tell us, maybe get it from config.
        SocketIo = new SocketIOClient.SocketIO($"http://{_settings?.Server.Addr}:{_settings?.Server.Port}");

        SocketIo.On("welcome", async _ =>
        {
            try
            {
                await SocketIo.EmitAsync("loaded", new Outbound.Loaded(
                    true,
                    1920,
                    1080,
                    2));
            }
            finally
            {
                _welcomeSemaphore.Release();
            }
        });

        SocketIo.On("entities", async e =>
        {
            if (_authenticated) return;
            try
            {
                await SocketIo.EmitAsync("auth", new Outbound.Auth(
                    _settings?.AuthToken!,
                    _settings?.Character.Id!,
                    _settings?.UserId!,
                    1920,
                    1080,
                    2,
                    false,
                    false
                ));

                _authenticated = true;
            }
            finally
            {
                _entitiesSemaphore.Release();
            }
        });

        SocketIo.On("start", e =>
        {
            try
            {
                OnConnected?.Invoke(e.GetValue<JsonElement>());
                _ready = true;
            }
            finally
            {
                _startSemaphore.Release();
            }
        });

        SocketIo.OnDisconnected += async (_, e) => await HandleError("_socketIo.OnDisconnected", e);
        SocketIo.OnError += async (_, e) => await HandleError("_socketIo.OnError", e);

        await SocketIo.ConnectAsync();
        await _welcomeSemaphore.WaitAsync();
        await _entitiesSemaphore.WaitAsync();
        await _startSemaphore.WaitAsync();

        _connected = true;
    }

    private async Task HandleError(string type, object e)
    {
        _logger.LogError("{Type}: {E}", type, e);

        // Release the semaphores to ensure StartConnection doesn't get stuck.
        _welcomeSemaphore.Release();
        _entitiesSemaphore.Release();
        _startSemaphore.Release();

        await CloseExistingConnection();
    }

    private async Task CloseExistingConnection()
    {
        if (_ready)
        {
            // OnDisconnected calls HandleError that then calls this, not sure that's the best idea, but it shouldn't hurt.
            OnDisconnected?.Invoke();
            _ready = false;
        }

        try
        {
            if (SocketIo is not null)
            {
                await SocketIo.DisconnectAsync();
                SocketIo?.Dispose();
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error disposing _socketIo: {E}", e);
        }
        finally
        {
            SocketIo = null;
            _authenticated = false;
            _connected = false;
        }
    }
}