using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.Core.SocketApi;

public class Socket : IDisposable {
    public bool Connected => _connection.Connected && _player.Id != string.Empty;
    public event Action<string, object>? OnEmit;
    public event Action<string, object>? OnRecv;

    public SocketEntityData Player => _player.Data;
    public ISocketEntityMovementPlan? PlayerMovementPlan { 
        get => _playerMovementPlan;
        set {
            if (value == null) {
                _player.TargetPosition = null;
                _player.Moving = false;
            }
            _playerMovementPlan = value;
        }
    }
    public SocketPlayerInventory PlayerInventory => _playerInventory;
    public SocketPlayerEquipment PlayerEquipment => _playerEquipment;
    public IEnumerable<SocketEntityData> Entities => _entities.Values.Select(x => x.Data);
    public IReadOnlyDictionary<string, SocketDropData> Drops => _drops;
    public IEnumerable<SocketEntityData> Party => _party.Where(_entities.ContainsKey).Select(x => _entities[x].Data);

    public Socket(
        GameData gameData,
        ConnectionSettings settings,
        Action<string, object>? fnOnEmit = null,
        Action<string, object>? fnOnRecv = null)
    {
        _gameData = gameData;
        _connection = new(settings);

        _connection.OnConnected += (x) => {
            Log.Info("Connected to server.");

            MethodInfo? fnRecvPlayerEvent = typeof(Socket).GetMethod("RecvPlayerEvent",
                BindingFlags.Instance | BindingFlags.NonPublic, [ typeof(Dictionary<string, JsonElement>) ]);
            Debug.Assert(fnRecvPlayerEvent != null, "Could not find method to handle player event.");
            _recvQueue.Enqueue(("player", fnRecvPlayerEvent, x));

            _connection.SocketIo!.On("player", e => {
                Dictionary<string, JsonElement> data = e.GetValue<Dictionary<string, JsonElement>>();
                _recvQueue.Enqueue(("player", fnRecvPlayerEvent, data));
            });
        };

        _connection.OnDisconnected += () => {
            Log.Info("Disconnected from server.");
        };

        OnEmit += fnOnEmit;
        OnRecv += fnOnRecv;

        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()) {
            InboundSocketMessageAttribute? evt = type.GetCustomAttribute<InboundSocketMessageAttribute>();
            if (evt == null) {
                continue;
            }

            MethodInfo? method = typeof(Socket).GetMethod("Recv",
                BindingFlags.Instance | BindingFlags.NonPublic, [ type ]);

            if (method == null) {
                Log.Info($"Could not find method to handle message {evt.Name}.");
                continue;
            }

            MethodInfo? socketGetValueMethod = typeof(SocketIOClient.SocketIOResponse)
                .GetMethod("GetValue", BindingFlags.Instance | BindingFlags.Public);
            Debug.Assert(socketGetValueMethod != null, "Could not find GetValue method on SocketIOResponse.");

            MethodInfo? genericMethod = socketGetValueMethod.MakeGenericMethod(type);
            Debug.Assert(genericMethod != null, "Could not create generic method for GetValue.");

            _connection.OnConnected += _ => {
                _connection.SocketIo!.On(evt.Name, e => {
                    object? data = genericMethod.Invoke(e, [0]);
                    Debug.Assert(data != null, "Could not get data from SocketIOResponse.");
                    _recvQueue.Enqueue((evt.Name, method, data));
                });
            };
        }
    }

    public void Dispose() {
        _connection.Dispose();
    }

    public Task Emit<T>(T evt) where T: struct {
        string name = typeof(T).GetCustomAttribute<OutboundSocketMessageAttribute>()?.Name
            ?? throw new InvalidOperationException("Not a valid OutboundSocketMessage.");

        try {
            OnEmit?.Invoke(name, evt);
        } catch (Exception ex) {
            Log.Error($"(user) Error emitting message {name}: {ex}");
        }

        return _connection.SocketIo!.EmitAsync(name, evt);
    }

    public void Update() {
        _connection.Update();

        while (_recvQueue.TryDequeue(out (string evt, MethodInfo method, object data) queued)) {
            try {
                queued.method.Invoke(this, [queued.data]);
            } catch (Exception ex) {
                Log.Error($"(system) Error processing message {queued.evt}: {ex}");
            }

            try {
                OnRecv?.Invoke(queued.evt, queued.data);
            } catch (Exception ex) {
                Log.Error($"(user) Error processing message {queued.evt}: {ex}");
            }
        }

        if (!Connected) {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        double dt = Math.Min(now.Subtract(_entitiesMoveUpdateLast).TotalSeconds, 0.1f);
        _entitiesMoveUpdateLast = now;

        HashSet<string> finishedEntities = [];

        foreach (KeyValuePair<string, ISocketEntityMovementPlan> kvp in _entityMovementPlans) {
            if (!_entities.TryGetValue(kvp.Key, out SocketEntity? e)) {
                continue;
            }

            bool finished = kvp.Value.Update(dt, e.Speed);
            e.Position = kvp.Value.Position;
            e.TargetPosition = kvp.Value.Goal;

            if (finished) {
                finishedEntities.Add(kvp.Key);
            }
        }

        foreach (string id in finishedEntities) {
            _entityMovementPlans.Remove(id);
        }

        if (PlayerMovementPlan != null) {
            bool finished = PlayerMovementPlan.Update(dt, _player.Speed);
            _player.Position = PlayerMovementPlan.Position;
            _player.TargetPosition = PlayerMovementPlan.Goal;

            if (finished) {
                PlayerMovementPlan = null;
            }
        }

        TimeSpan playerMoveInterval = TimeSpan.FromSeconds(1.0f / 10.0f);

        if (_player.TargetPosition.HasValue && now.Subtract(_playerMoveUpdateLast) >= playerMoveInterval) {
            Emit<Outbound.Move>(new(
                X: _player.Position.X,
                Y: _player.Position.Y,
                TargetX: _player.TargetPosition.Value.X,
                TargetY: _player.TargetPosition.Value.Y,
                MapId: _player.MapId));

            _playerMoveUpdateLast = now;

            if (_player.TargetPosition == _player.Position) {
                _player.TargetPosition = null;
            }
        }
    }

    private readonly GameData _gameData;
    private readonly Connection _connection;

    private ConcurrentQueue<(string, MethodInfo, object)> _recvQueue = [];
    private DateTimeOffset _playerMoveUpdateLast = DateTimeOffset.UtcNow;
    private DateTimeOffset _entitiesMoveUpdateLast = DateTimeOffset.UtcNow;

    private readonly Dictionary<string, SocketEntity> _entities = [];
    private readonly Dictionary<string, ISocketEntityMovementPlan> _entityMovementPlans = [];
    private readonly Dictionary<string, SocketDropData> _drops = [];

    private List<string> _party = [];

    private readonly SocketEntity _player = new();
    private ISocketEntityMovementPlan? _playerMovementPlan;
    private SocketPlayerInventory _playerInventory = new(0, []);
    private SocketPlayerEquipment _playerEquipment;

    private void UpdateOrCreateEntity(Dictionary<string, JsonElement> source, SocketEntityType type) {
        string id = source["id"].GetString()!;

        if (!_entities.TryGetValue(id, out SocketEntity? e)) {
            e = new() {
                Id = id,
                Type = type,
                TypeString = source.TryGetValue("type", out JsonElement typeElem) ? typeElem.GetString()! : string.Empty
            };

            _entities.Add(id, e);
        }

        e.Update(source, type == SocketEntityType.Monster ? _gameData.Monsters.GetValueOrDefault(e.TypeString) : null);

        if (e.Moving && e.TargetPosition.HasValue) {
            _entityMovementPlans[e.Id] = new DestinationMovementPlan(e.Position, e.TargetPosition.Value);
        }
    }

    private void Recv(Inbound.ActionData evt) {
        /* TODO */
    }

    private void Recv(Inbound.ChatMessageData evt) {
        Log.Info($"{evt.Id}: {evt.Message}");
    }

    private void Recv(Inbound.ChestOpenedData evt) {
        if (evt.Gone) {
            _drops.Remove(evt.Id);
        }
    }

    private void Recv(Inbound.CorrectionData evt) {
        _player.Position = new((float)evt.X, (float)evt.Y);
    }

    private void Recv(Inbound.DeathData evt) {
        if (!string.IsNullOrWhiteSpace(evt.Id)) {
            if (evt.Id == _player.Id) {
                _player.TargetPosition = null;
            }

            _entities.Remove(evt.Id);
        }
    }

    private void Recv(Inbound.DisappearData evt) {
        if (!string.IsNullOrWhiteSpace(evt.Id)) {
            if (evt.Id == _player.Id) {
                _player.TargetPosition = null;
            }

            _entities.Remove(evt.Id);
        }
    }

    private void Recv(Inbound.ChestDropData evt) {
        _drops.Add(evt.Id, new(evt.Id, evt.X, evt.Y));
    }

    private void Recv(Inbound.EntitiesData evt) {
        if (evt.Type == "all") {
            _entities.Clear();
        }

        foreach (Dictionary<string, JsonElement> player in evt.Players) {
            UpdateOrCreateEntity(player, SocketEntityType.Player);
        }

        foreach (Dictionary<string, JsonElement> monster in evt.Monsters) {
            UpdateOrCreateEntity(monster, SocketEntityType.Monster);
        }
    }

    private void Recv(Inbound.GameEventData evt) {
        Log.Info($"{evt.Name} on {evt.Map}");
    }

    private void Recv(Inbound.GameResponseData evt) {
        /* TODO */
        Log.Debug($"{evt.Response}\n{evt}");
    }

    private void Recv(Inbound.HitData evt) {
        /* TODO */
    }

    private void Recv(Inbound.PartyInviteData evt) {
        Log.Info($"Party invite from {evt.Name}");
    }

    private void Recv(Inbound.MagiportData evt) {
        Log.Info($"Magiporting from {evt.Name}");
    }

    private void Recv(Inbound.NewMapData evt) {
        Debug.Assert(evt.Entities.Type == "all");
        Recv(evt.Entities);

        _player.Map = evt.MapName;
        _player.MapId = evt.MapId;
        _player.Position = new((float)evt.PlayerX, (float)evt.PlayerY);
        _player.TargetPosition = null;
    }

    private void Recv(Inbound.PartyRequestData evt) {
        Log.Info($"Party request from {evt.Name}");
    }

    private void Recv(Inbound.PartyUpdateData evt) {
        _party = evt.Members ?? [];
    }

    private void Recv(Inbound.ServerMessageData evt) {
        Log.Info($"{evt.Message}");
    }

    private void Recv(Inbound.UpgradeData evt) {
        Log.Info($"Upgrade {evt.Type} {(evt.Success ? "succeeded" : "failed")}");
    }

    private void Recv(Inbound.WelcomeData evt) {
        Log.Info($"Welcome {evt.Name} to {evt.Map}");
    }

    private void RecvPlayerEvent(Dictionary<string, JsonElement> player) {
        Vector2 position = _player.Position;
        Vector2? targetPosition = _player.TargetPosition;

        _player.Update(player);

        if (player.TryGetValue("gold", out JsonElement gold)) {
            _playerInventory = _playerInventory with { Gold = gold.GetDouble() };
        }

        if (player.TryGetValue("items", out JsonElement items)) {
            _playerInventory = _playerInventory with { Items = [..items
                .EnumerateArray()
                .Select((x, i) => (x, i))
                .Where(x => x.x.ValueKind != JsonValueKind.Null)
                .Select(x => new SocketInventoryItem(x.x, x.i))
            ]};
        }

        if (player.TryGetValue("slots", out JsonElement slots)) {
            _playerEquipment = new(slots);
        }

        if (position != default && Vector2.Distance(_player.Position, position) <= 100.0f) { // trust local position
            _player.Position = position;
        }

        _player.TargetPosition = targetPosition;
    }
}
