namespace AdventureLandSharp.Core.SocketApi;

public class Socket : IDisposable
{
    private readonly SocketConnection _connection;

    private readonly Dictionary<string, DropData> _drops = [];

    private readonly Dictionary<string, Entity> _entities = [];
    private readonly GameData _gameData;

    private readonly ILogger _logger;

    private readonly ConcurrentQueue<(string, MethodInfo, object)> _receiveQueue = [];
    private DateTimeOffset _lastNetMove = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastTick = DateTimeOffset.UtcNow;

    private List<string> _party = [];

    public Socket(
        ref readonly GameData gameData,
        SocketConnection connection,
        ILogger<Socket> logger)
    {
        _gameData = gameData;
        _connection = connection;
        _logger = logger;

        _connection.OnConnected += x =>
        {
            _logger.LogInformation("Connected to server");

            var receivePlayer = typeof(Socket).GetMethod(
                nameof(ReceivePlayer),
                BindingFlags.Instance | BindingFlags.NonPublic,
                [typeof(JsonElement)]
            );

            Debug.Assert(receivePlayer != null);
            Player = new LocalPlayer(x);

            _connection.SocketIo!.On("player",
                e => { _receiveQueue.Enqueue(("player", receivePlayer, e.GetValue<JsonElement>())); });
        };

        _connection.OnDisconnected += () => { _logger.LogInformation("Disconnected from server"); };

        foreach (var (type, method, name) in Assembly.GetExecutingAssembly().GetTypes()
                     .Select(x => (type: x, attr: x.GetCustomAttribute<InboundSocketMessageAttribute>()))
                     .Where(x => x.attr != null)
                     .Select(x => (
                         x.type,
                         name: x.attr!.Name,
                         method: typeof(Socket).GetMethod("Receive", BindingFlags.Instance | BindingFlags.NonPublic,
                             [x.type]
                         )))
                     .Where(x => x.method != null)
                     .Select(x => (x.type, x.method!, x.name))
                )
        {
            _logger.LogInformation("Registered message {Name} to handler {MethodName}", name, method.Name);

            var socketGetValueMethod = typeof(SocketIOResponse).GetMethod(
                nameof(SocketIOResponse.GetValue),
                BindingFlags.Instance | BindingFlags.Public
            );
            Debug.Assert(socketGetValueMethod != null, "Could not find GetValue method on SocketIOResponse.");

            var genericMethod = socketGetValueMethod.MakeGenericMethod(type);
            Debug.Assert(genericMethod != null, "Could not create generic method for GetValue.");

            _connection.OnConnected += _ =>
            {
                _connection.SocketIo!.On(name, e =>
                {
                    var data = genericMethod.Invoke(e, [0]);
                    Debug.Assert(data != null, "Could not get data from SocketIOResponse.");
                    _receiveQueue.Enqueue((name, method, data));
                });
            };
        }
    }

    public bool Connected => _connection.Connected && Player.Id != string.Empty;

    public LocalPlayer Player { get; private set; } = default!;

    public IEnumerable<Entity> Entities => _entities.Values;
    public IEnumerable<DropData> Drops => _drops.Values;

    public IEnumerable<Entity> Party => _party
        .Where(_entities.ContainsKey)
        .Select(x => _entities[x]);

    public void Dispose()
    {
        _connection.Dispose();

        GC.SuppressFinalize(this);
    }

    public event Action<string, object>? OnEmit;
    public event Action<string, object>? OnReceive;

    public async Task Emit<T>(T evt) where T : struct
    {
        var name = typeof(T).GetCustomAttribute<OutboundSocketMessageAttribute>()?.Name
                   ?? throw new InvalidOperationException("Not a valid OutboundSocketMessage.");

        try
        {
            OnEmit?.Invoke(name, evt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "(user) Error emitting message {Name}: {Ex}", name, ex);
        }

        await _connection.SocketIo!.EmitAsync(name, evt);
    }

    public async Task Update()
    {
        await _connection.Update();

        UpdateDrainReceiveQueue();
        UpdateCullEntities();

        if (!Connected) return;

        UpdateTick();
        await UpdateNetMovement();
    }

    private void Receive(Inbound.ChestOpenedData evt)
    {
        if (evt.Gone) _drops.Remove(evt.Id);
    }

    private void Receive(Inbound.CorrectionData evt)
    {
        Player.On(evt);
    }

    private void Receive(Inbound.DeathData evt)
    {
        if (evt.Id == Player.Id) Player.On(evt);

        if (_entities.TryGetValue(evt.Id, out var e)) e.On(evt);
    }

    private void Receive(Inbound.DisappearData evt)
    {
        _entities.Remove(evt.Id);
    }

    private void Receive(Inbound.ChestDropData evt)
    {
        if (evt.Owners.Contains(Player.OwnerId)) _drops.Add(evt.Id, new DropData(evt.Id, evt.X, evt.Y));
    }

    private void Receive(Inbound.EntitiesData evt)
    {
        if (evt.Type == "all") _entities.Clear();

        foreach (var player in evt.Players)
        {
            var id = player.GetString("id");

            if (id == Player.Id)
            {
                ReceivePlayer(player);
                continue;
            }

            if (_entities.TryGetValue(id, out var e))
                e.Update(player);
            else
                _entities.Add(id, id.StartsWith('$') ? new Npc(player) : new Player(player));
        }

        foreach (var monster in evt.Monsters)
        {
            var id = monster.GetString("id");

            if (_entities.TryGetValue(id, out var e))
            {
                e.Update(monster);
            }
            else
            {
                var monsterDef = _gameData.Monsters[monster.GetString("type")];
                _entities.Add(id, new Monster(monster, monsterDef));
            }
        }
    }

    private void Receive(Inbound.NewMapData evt)
    {
        Player.On(evt);
        Receive(evt.Entities);
    }

    private void Receive(Inbound.PartyUpdateData evt)
    {
        _party = evt.Members ?? [];
    }

    private void ReceivePlayer(JsonElement data)
    {
        Player.Update(data);
    }

    private void UpdateDrainReceiveQueue()
    {
        while (_receiveQueue.TryDequeue(out (string evt, MethodInfo method, object data) queued))
        {
            try
            {
                queued.method.Invoke(this, [queued.data]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "(system) Error processing message {QueuedEvent}: {Ex}", queued.evt, ex);
            }

            try
            {
                OnReceive?.Invoke(queued.evt, queued.data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "(user) Error processing message {QueuedEvent}: {Ex}", queued.evt, ex);
            }
        }
    }

    private void UpdateCullEntities()
    {
        List<string> cull = [];

        foreach (var (key, e) in _entities)
        {
            var ex = e.Position.X;
            var ey = e.Position.Y;
            var px = Player.Position.X;
            var py = Player.Position.Y;

            var cullOnX = ex < px - GameConstants.VisionWidth || ex > px + GameConstants.VisionWidth;
            var cullOnY = ey < py - GameConstants.VisionHeight || ey > py + GameConstants.VisionHeight;

            if (cullOnX || cullOnY) cull.Add(key);
        }

        foreach (var key in cull) _entities.Remove(key);
    }

    private void UpdateTick()
    {
        var now = DateTimeOffset.UtcNow;
        var dt = MathF.Min((float) now.Subtract(_lastTick).TotalSeconds, 1.0f);
        _lastTick = now;

        Player.Tick(dt);

        foreach (var e in _entities.Values) e.Tick(dt);
    }

    public async Task UpdateNetMovement()
    {
        var now = DateTimeOffset.UtcNow;

        var timeSinceLastMove = now.Subtract(_lastNetMove);
        var moveInterval = TimeSpan.FromSeconds(1.0f / 10.0f);

        if (Player.RemotePosition == Player.GoalPosition || timeSinceLastMove < moveInterval) return;

        await Emit(new Outbound.Move(
            Player.Position.X,
            Player.Position.Y,
            Player.GoalPosition.X,
            Player.GoalPosition.Y,
            Player.MapId));

        _lastNetMove = now;
    }
}