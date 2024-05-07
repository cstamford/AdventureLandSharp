using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using AdventureLandSharp.Core.Util;
using SocketIOClient;

namespace AdventureLandSharp.Core.SocketApi;

public class Socket : IDisposable {
    public bool Connected => _connection.Connected && _player.Id != null;

    public event Action<JsonElement>? OnGameResponse;
    public event Action<Inbound.HitData>? OnHit;
    public event Action<Inbound.MagiportRequestData>? OnMagiportRequest;
    public event Action<Inbound.PartyRequestData>? OnPartyRequest;
    public event Action<Inbound.ServerInfo>? OnServerInfo;
    public event Action<Inbound.SkillTimeoutData>? OnSkillTimeout;

    public LocalPlayer Player => _player;
    public IEnumerable<Entity> Entities => _entities.Values;
    public IEnumerable<DropData> Drops => _drops.Values;
    public IEnumerable<string> Party => _party;
    public IEnumerable<Player> PartyInRange => _party
        .Where(_entities.ContainsKey)
        .Select(x => _entities[x])
        .Cast<Player>();

    public Inbound.ServerInfo ServerInfo;

    public Socket(
        GameData gameData,
        ConnectionSettings settings)
    {
        _log = new(settings.Character.Name, "SOCKET");
        _gameData = gameData;
        _connection = new(settings);

        HashSet<string> handledEvents = [];

        foreach ((Type type, MethodInfo method, string name, bool debug) in Assembly.GetExecutingAssembly().GetTypes()
            .Select(x => (type: x, attr: x.GetCustomAttribute<InboundSocketMessageAttribute>()))
            .Where(x => x.attr != null)
            .Select(x => (
                x.type,
                method: typeof(Socket).GetMethod("Recv", BindingFlags.Instance | BindingFlags.NonPublic, [ x.type ]),
                name: x.attr!.Name,
                debug: x.attr!.Debug
            ))
            .Where(x => x.method != null)
            .Select(x => (x.type, x.method!, x.name, x.debug))
        ) {
            MethodInfo? socketGetValueMethod = typeof(SocketIOResponse).GetMethod(nameof(SocketIOResponse.GetValue), BindingFlags.Instance | BindingFlags.Public);
            Debug.Assert(socketGetValueMethod != null, "Could not find GetValue method on SocketIOResponse.");

            MethodInfo? genericMethod = socketGetValueMethod.MakeGenericMethod(type);
            Debug.Assert(genericMethod != null, "Could not create generic method for GetValue.");

            _connection.OnConnected += _ => {
                RegisterRecv_Queue(name, e => {
                    if (debug) {
                        _log.Debug($"{name}: {e}");
                    }

                    object? data = genericMethod.Invoke(e, [0]);
                    Debug.Assert(data != null, "Could not get data from SocketIOResponse.");
                    method.Invoke(this, [data]);
                });
            };

            handledEvents.Add(name);
        }

        _connection.OnCreateSocket += () => {
            _connection.OnAny((name, e) => {
                if (handledEvents.Contains(name)) {
                    if (Log.LogLevelEnabled(LogLevel.DebugVerbose)) {
                        _log.DebugVerbose(["RECV"], $"{name} {e}");
                    }
                } else {
                    if (Log.LogLevelEnabled(LogLevel.Debug)) {
                        _log.Debug(["RECV", "UNKNOWN"], $"{name} {e}");
                    }
                }
            });
            

            RegisterRecv_NoQueue("disconnect_reason", Recv_NoQueue_DisconnectReason);
            RegisterRecv_NoQueue("limitdcreport", Recv_NoQueue_LimitDCReport);
            RegisterRecv_NoQueue("start", Recv_NoQueue_Start);
            RegisterRecv_NoQueue("welcome", Recv_NoQueue_Welcome);
        };

        _connection.OnConnected += (e) => {
            _log.Info($"Connected to server.");
            _player = new LocalPlayer(e);

            RegisterRecv_Queue("game_response", Recv_GameResponse);
            RegisterRecv_Queue("player", Recv_Player);
        };

        _connection.OnDisconnected += () => {
            _log.Info($"Disconnected from server.");
        };
    }

    public void Dispose() {
        _connection.Dispose();
    }

    public Task Emit<T>(T evt) where T: struct {
        string name = typeof(T).GetCustomAttribute<OutboundSocketMessageAttribute>()?.Name
            ?? throw new InvalidOperationException("Not a valid OutboundSocketMessage.");

        if (Log.LogLevelEnabled(LogLevel.DebugVerbose)) {
            _log.DebugVerbose(["SEND"], $"{name} {evt}");
        }

        return _connection.EmitAsync(name, evt);
    }

    public bool TryGetEntity(string id, out Entity e) => _entities.TryGetValue(id, out e!);

    public void Update() {
        _connection.Update();

        if (!Connected) {
            return;
        }

        Update_DrainRecvQueue();
        Update_CullEntities();
        Update_Tick();
        Update_NetMovement();
    }

    private readonly GameData _gameData;
    private readonly Connection _connection;
    private readonly Logger _log;

    private readonly record struct QueuedSocketData(string Event, Action<SocketIOResponse> Handler, SocketIOResponse Data);
    private readonly ConcurrentQueue<QueuedSocketData> _recvQueue = [];

    private DateTimeOffset _lastNetMove = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastTick = DateTimeOffset.UtcNow;

    private readonly Dictionary<string, Entity> _entities = [];
    private readonly Dictionary<string, DropData> _drops = [];

    private string[] _party = [];
    private LocalPlayer _player = default!;
    private Inbound.ServerInfo _serverInfo = default!;

    private void Recv(Inbound.CorrectionData evt) {
        _player.On(evt);
    }

    private void Recv(Inbound.DeathData evt) {
        if (evt.Id == _player.Id) {
            _player.On(evt);
        }

        _entities.Remove(evt.Id);
    }

    private void Recv(Inbound.DisappearData evt) {
        _entities.Remove(evt.Id);
    }

    private void Recv(Inbound.ChestDropData evt) {
        if (evt.Owners.Contains(_player.OwnerId)) {
            _drops.Add(evt.Id, new(evt.Id, evt.X, evt.Y));
        }
    }

    private void Recv(Inbound.ChestOpenedData evt) {
        _drops.Remove(evt.Id);
    }

    private void Recv(Inbound.EntitiesData evt) {
        if (!Connected) {
            return;
        }

        if (evt.Type == "all") {
            _entities.Clear();
        }

        foreach (JsonElement player in evt.Players) {
            string id = player.GetString("id");

            if (id == _player.Id) {
                _player.Update(player);
                continue;
            }

            if (_entities.TryGetValue(id, out Entity? e)) {
                e.Update(player);
            } else {
                _entities.Add(id, id.StartsWith('$') ? new Npc(player) : new Player(player));
            }
        }

        foreach (JsonElement monster in evt.Monsters) {
            string id = monster.GetString("id");

            if (_entities.TryGetValue(id, out Entity? e)) {
                e.Update(monster);
            } else {
                string type = monster.GetString("type");
                GameDataMonster monsterDef = _gameData.Monsters[type];
                _entities.Add(id, new Monster(monster, monsterDef, _gameData.GetMonsterSize(type)));
            }
        }
    }

    private void Recv(Inbound.HitData evt) {
        OnHit?.Invoke(evt);
    }

    private void Recv(Inbound.MagiportRequestData evt) {
        OnMagiportRequest?.Invoke(evt);
    }

    private void Recv(Inbound.NewMapData evt) {
        _player.On(evt);
        Recv(evt.Entities);
    }

    private void Recv(Inbound.PartyUpdateData evt) {
        _party = evt.Members ?? [];
    }

    private void Recv(Inbound.PartyRequestData evt) {
        OnPartyRequest?.Invoke(evt);
    }

    private void Recv(Inbound.ServerInfo evt) {
        OnServerInfo?.Invoke(evt);
    }

    private void Recv(Inbound.SkillTimeoutData evt) {
        OnSkillTimeout?.Invoke(evt);
    }

    private void Recv_GameResponse(SocketIOResponse data) {
        OnGameResponse?.Invoke(data.GetValue<JsonElement>());
    }

    private void Recv_Player(SocketIOResponse data) {
        _player.Update(data.GetValue<JsonElement>());
    }

    private void Recv_NoQueue_DisconnectReason(SocketIOResponse data) {
        _log.Warn($"disconnect_reason: {data}");
    }

    private void Recv_NoQueue_LimitDCReport(SocketIOResponse data) {
        _log.Warn($"disconnect_reason: {data}");
    }

    private void Recv_NoQueue_Start(SocketIOResponse data) {
        JsonElement root = data.GetValue<JsonElement>();
        _serverInfo = root.GetProperty("s_info").Deserialize<Inbound.ServerInfo>();
    }

    private void Recv_NoQueue_Welcome(SocketIOResponse data) {
        JsonElement root = data.GetValue<JsonElement>();
        _serverInfo = root.GetProperty("S").Deserialize<Inbound.ServerInfo>();
    }

    private void RegisterRecv_NoQueue(string name, Action<SocketIOResponse> handler) {
        _connection.On(name, handler);
    }

    private void RegisterRecv_Queue(string name, Action<SocketIOResponse> handler) {
        _connection.On(name, e => _recvQueue.Enqueue(new(name, handler, e)));
    }

    private void Update_DrainRecvQueue() {
        while (_recvQueue.TryDequeue(out QueuedSocketData data)) {
            try {
                data.Handler(data.Data);
            } catch (Exception ex) {
                _log.Error($"Error processing incoming message {data.Event}: {ex}");
            }
        }
    }

    private void Update_CullEntities() {
        List<string> cull = [];

        foreach ((string key, Entity e) in _entities) {
            float ex = e.Position.X;
            float ey = e.Position.Y;
            float px = _player.Position.X;
            float py = _player.Position.Y;

            bool cullOnX = ex < px - GameConstants.VisionWidth || ex > px + GameConstants.VisionWidth;
            bool cullOnY = ey < py - GameConstants.VisionHeight || ey > py + GameConstants.VisionHeight;

            if (cullOnX || cullOnY) {
                cull.Add(key);
            }
        }

        foreach (string key in cull) {
            _entities.Remove(key);
        }
    }

    private void Update_Tick() {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        float dt = MathF.Min((float)now.Subtract(_lastTick).TotalSeconds, 1.0f);
        _lastTick = now;

        _player.Tick(dt);

        foreach (Entity e in _entities.Values) {
            e.Tick(dt);
        }
    }

    public void Update_NetMovement() {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        TimeSpan timeSinceLastMove = now.Subtract(_lastNetMove);
        TimeSpan moveInterval = TimeSpan.FromSeconds(1.0f / 10.0f);

        if (_player.GoalPosition != _player.RemoteGoalPosition && timeSinceLastMove >= moveInterval) {
            Emit<Outbound.Move>(new(
                X: _player.Position.X,
                Y: _player.Position.Y,
                TargetX: _player.GoalPosition.X,
                TargetY: _player.GoalPosition.Y,
                MapId: _player.MapId
            ));

            _player.RemoteGoalPosition = _player.GoalPosition;
            _lastNetMove = now;
        }
    }
}
