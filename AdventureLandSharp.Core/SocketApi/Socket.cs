using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.Core.SocketApi;

public class Socket : IDisposable {
    public bool Connected => _connection.Connected && _player.Id != string.Empty;
    public event Action<string, object>? OnEmit;
    public event Action<string, object>? OnRecv;

    public LocalPlayer Player => _player;
    public IEnumerable<Entity> Entities => _entities.Values;
    public IEnumerable<DropData> Drops => _drops.Values;
    public IEnumerable<Entity> Party => _party
        .Where(_entities.ContainsKey)
        .Select(x => _entities[x]);

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

            MethodInfo? recvPlayer = typeof(Socket).GetMethod(
                nameof(Recv_Player),
                BindingFlags.Instance | BindingFlags.NonPublic,
                [ typeof(JsonElement) ]
            );

            Debug.Assert(recvPlayer != null);
            _player = new LocalPlayer(x);

            _connection.SocketIo!.On("player", e => {
                _recvQueue.Enqueue(("player", recvPlayer, e.GetValue<JsonElement>()));
            });
        };

        _connection.OnDisconnected += () => {
            Log.Info("Disconnected from server.");
        };

        OnEmit += fnOnEmit;
        OnRecv += fnOnRecv;

        foreach ((Type type, MethodInfo method, string name) in Assembly.GetExecutingAssembly().GetTypes()
            .Select(x => (type: x, attr: x.GetCustomAttribute<InboundSocketMessageAttribute>()))
            .Where(x => x.attr != null)
            .Select(x => (
                x.type,
                name: x.attr!.Name,
                method: typeof(Socket).GetMethod("Recv", BindingFlags.Instance | BindingFlags.NonPublic, [ x.type ]
            )))
            .Where(x => x.method != null)
            .Select(x => (x.type, x.method!, x.name))
        ) {
            Log.Info($"Registered message {name} to handler {method.Name}.");

            MethodInfo? socketGetValueMethod = typeof(SocketIOClient.SocketIOResponse).GetMethod(
                nameof(SocketIOClient.SocketIOResponse.GetValue),
                BindingFlags.Instance | BindingFlags.Public
            );
            Debug.Assert(socketGetValueMethod != null, "Could not find GetValue method on SocketIOResponse.");

            MethodInfo? genericMethod = socketGetValueMethod.MakeGenericMethod(type);
            Debug.Assert(genericMethod != null, "Could not create generic method for GetValue.");

            _connection.OnConnected += _ => {
                _connection.SocketIo!.On(name, e => {
                    object? data = genericMethod.Invoke(e, [0]);
                    Debug.Assert(data != null, "Could not get data from SocketIOResponse.");
                    _recvQueue.Enqueue((name, method, data));
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

        Update_DrainRecvQueue();
        Update_CullEntities();

        if (!Connected) {
            return;
        }

        Update_Tick();
        Update_NetMovement();
    }

    private readonly GameData _gameData;
    private readonly Connection _connection;

    private readonly ConcurrentQueue<(string, MethodInfo, object)> _recvQueue = [];
    private DateTimeOffset _lastNetMove = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastTick = DateTimeOffset.UtcNow;

    private readonly Dictionary<string, Entity> _entities = [];
    private readonly Dictionary<string, DropData> _drops = [];

    private List<string> _party = [];
    private LocalPlayer _player = default!;

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
                GameDataMonster monsterDef = _gameData.Monsters[monster.GetString("type")];
                _entities.Add(id, new Monster(monster, monsterDef));
            }
        }
    }

    private void Recv(Inbound.LimitDcReportData evt) {
        Log.Info($"Limit DC Report: {evt}");
    }

    private void Recv(Inbound.NewMapData evt) {
        _player.On(evt);
        Recv(evt.Entities);
    }

    private void Recv(Inbound.PartyUpdateData evt) {
        _party = evt.Members ?? [];
    }

    private void Recv_Player(JsonElement data) {
        _player.Update(data);
    }

    private void Update_DrainRecvQueue() {
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
                MapId: _player.MapId));

            _player.RemoteGoalPosition = _player.GoalPosition;
            _lastNetMove = now;
        }
    }
}
