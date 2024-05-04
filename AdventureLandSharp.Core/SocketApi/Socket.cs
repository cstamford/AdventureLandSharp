using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.Core.SocketApi;

public class Socket : IDisposable {
    public bool Connected => _connection.Connected && _player.Id != null;

    public event Action<string, object>? OnEmit;
    public event Action<string, object>? OnRecv;

    public event Action<JsonElement>? OnGameResponse;
    public event Action<Inbound.HitData>? OnHit;
    public event Action<Inbound.MagiportRequestData>? OnMagiportRequest;
    public event Action<Inbound.PartyRequestData>? OnPartyRequest;
    public event Action<Inbound.SkillTimeoutData>? OnSkillTimeout;

    public LocalPlayer Player => _player;
    public IEnumerable<Entity> Entities => _entities.Values;
    public IEnumerable<DropData> Drops => _drops.Values;
    public IEnumerable<string> Party => _party;
    public IEnumerable<Player> PartyInRange => _party
        .Where(_entities.ContainsKey)
        .Select(x => _entities[x])
        .Cast<Player>();

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
            MethodInfo? socketGetValueMethod = typeof(SocketIOClient.SocketIOResponse).GetMethod(
                nameof(SocketIOClient.SocketIOResponse.GetValue),
                BindingFlags.Instance | BindingFlags.Public
            );
            Debug.Assert(socketGetValueMethod != null, "Could not find GetValue method on SocketIOResponse.");

            MethodInfo? genericMethod = socketGetValueMethod.MakeGenericMethod(type);
            Debug.Assert(genericMethod != null, "Could not create generic method for GetValue.");

            _connection.OnConnected += _ => _connection.On(name, e => LowLevelRecv(name, e, debug, genericMethod, method));
            handledEvents.Add(name);
        }

        _connection.OnCreateSocket += () => {
            _connection.On("limitdcreport", e => _log.Error($"limitdcreport: {e}"));
            _connection.On("disconnect_reason", e => _log.Error($"disconnect_reason: {e}"));
        };

        _connection.OnConnected += (e) => {
            _log.Info($"Connected to server.");
            _player = new LocalPlayer(e);

            MethodInfo? recvPlayer = typeof(Socket).GetMethod(
                nameof(Recv_Player),
                BindingFlags.Instance | BindingFlags.NonPublic,
                [ typeof(JsonElement) ]
            );
            Debug.Assert(recvPlayer != null);
            _connection.On("player", e => _recvQueue.Enqueue(("player", recvPlayer, e.GetValue<JsonElement>())));


            MethodInfo? recvGameResponse = typeof(Socket).GetMethod(
                nameof(Recv_GameResponse),
                BindingFlags.Instance | BindingFlags.NonPublic,
                [ typeof(JsonElement) ]
            );
            Debug.Assert(recvGameResponse != null);
            _connection.On("game_response", e => _recvQueue.Enqueue(("game_response", recvGameResponse, e.GetValue<JsonElement>())));

            if (Log.LogLevelEnabled(LogLevel.Debug)) {
                _connection.OnAny((name, e) => {
                    if (!handledEvents.Contains(name)) {
                        _log.Debug(["RECV", "UNKNOWN"], $"{name} {e}");
                    }
                });
            }
        };

        _connection.OnDisconnected += () => {
            _log.Info($"Disconnected from server.");
        };

        if (Log.LogLevelEnabled(LogLevel.DebugVerbose)) {
            OnEmit += (evt, data) => _log.DebugVerbose(["SEND"], $"{evt} {data}");
            OnRecv += (evt, data) => _log.DebugVerbose(["RECV"], $"{evt} {data}");
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
            _log.Error($"(user) Error emitting message {name}: {ex}");
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

    private readonly ConcurrentQueue<(string, MethodInfo, object)> _recvQueue = [];
    private DateTimeOffset _lastNetMove = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastTick = DateTimeOffset.UtcNow;

    private readonly Dictionary<string, Entity> _entities = [];
    private readonly Dictionary<string, DropData> _drops = [];

    private string[] _party = [];
    private LocalPlayer _player = default!;

    private void LowLevelRecv(string evt, object e, bool withDebug, MethodInfo getTypedData, MethodInfo dispatchRecv) {
        try {
            if (Log.LogLevelEnabled(LogLevel.Debug) && withDebug) {
                _log.Debug($"{evt}: {e}");
            }

            object? data = getTypedData.Invoke(e, [0]);
            Debug.Assert(data != null, "Could not get data from SocketIOResponse.");
            _recvQueue.Enqueue((evt, dispatchRecv, data));
        } catch (Exception ex) {
            _log.Error($"(system) Error processing message {evt}: {ex}");
        }
    }

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
                Recv_Player(player);
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

    private void Recv(Inbound.SkillTimeoutData evt) {
        OnSkillTimeout?.Invoke(evt);
    }

    private void Recv_GameResponse(JsonElement data) {
        OnGameResponse?.Invoke(data);
    }

    private void Recv_Player(JsonElement data) {
        _player.Update(data);
    }

    private void Update_DrainRecvQueue() {
        while (_recvQueue.TryDequeue(out (string evt, MethodInfo method, object data) queued)) {
            try {
                OnRecv?.Invoke(queued.evt, queued.data);
            } catch (Exception ex) {
                _log.Error($"(user) Error processing message {queued.evt}: {ex}");
            }

            try {
                queued.method.Invoke(this, [queued.data]);
            } catch (Exception ex) {
                _log.Error($"(system) Error processing message {queued.evt}: {ex}");
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
