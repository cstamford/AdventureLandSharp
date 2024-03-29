﻿using AdventureLandSharp.Util;
using StackExchange.Redis;
using System.Text.Json;

namespace AdventureLandSharp.Socket;

public record struct SocketEntity(
    string Id,
    double X,
    double Y,
    double? TargetX,
    double? TargetY,
    double Speed);

public class SocketData(SocketIOClient.SocketIO client, IDatabase redis) {
    public void Update(double dt) {
        lock (_entities) {
            _player = UpdateEntityPosition(_player, dt);

            foreach (SocketEntity entity in _entities.Values) {
                _entities[entity.Id] = UpdateEntityPosition(entity, dt);
            }

            SocketEntity? stateForMove = default;
            _playerMoveAccumulator += dt;

            if (_playerMoveAccumulator >= 1.0) {
                stateForMove = _player;
            } else if (_playerMoveAccumulator >= 1.0 / 30.0 && _playerMoveQueue.TryDequeue(out SocketEntity move)) {
                stateForMove = move;
            }

            if (stateForMove is { TargetX: not null, TargetY: not null }) {
                Emit("move", new ClientToServer.Move(
                    X: stateForMove.Value.X,
                    Y: stateForMove.Value.Y,
                    TargetX: stateForMove.Value.TargetX!.Value,
                    TargetY: stateForMove.Value.TargetY!.Value,
                    MapId: _mapId
                ));

                _playerMoveAccumulator = 0.0;
            }
        }
    }

    public void UpdateFrom(ServerToClient.Correction correction) {
        lock (_entities) {
            _player = _player with { X = correction.X, Y = correction.Y };
        }
    }

    public void UpdateFrom(ServerToClient.Death death) {
        lock (_entities) {
            _entities.Remove(death.Id);
        }
    }

    public void UpdateFrom(ServerToClient.Disappear disappear) {
        lock (_entities) {
            _entities.Remove(disappear.Id);
        }
    }

    public void UpdateFrom(ServerToClient.Entities entities) {
        lock (_entities) {
            if (entities.Type == "full") {
                // TODO: Not sure if this is correct, but it seems correct. I bet we get a full every time we switch map.
                _entities.Clear();
            }

            foreach (ServerToClientTypes.Player player in entities.Players) {
                _entities[player.Id] = new(
                    Id: player.Id,
                    X: player.X,
                    Y: player.Y,
                    TargetX: player.Moving ? player.GoingX : null,
                    TargetY: player.Moving ? player.GoingY : null,
                    Speed: player.Speed
                );
            }

            foreach (ServerToClientTypes.Monster monster in entities.Monsters) {
                _entities[monster.Id] = new(
                    Id: monster.Id,
                    X: monster.X,
                    Y: monster.Y,
                    TargetX: monster.Moving ? monster.GoingX : null,
                    TargetY: monster.Moving ? monster.GoingY : null,
                    Speed: monster.Speed ?? 0
                );
            }
        }
    }

    public void UpdateFrom(ServerToClientTypes.Player player) {
        lock (_entities) {
            _player = _player with {
                X = player.X,
                Y = player.Y,
                Speed = player.Speed
            };

            if (player.Map != null) {
                _map = player.Map;
            }

            if (player.MapId.HasValue) {
                _mapId = player.MapId.Value;
            }
        }
    }

    public void UpdateFrom(ServerToClient.Welcome welcome) {
        lock (_entities) {
            _map = welcome.Map;
        }
    }

    public void UpdatePlayerGoal(double goalX, double goalY) {
        lock (_entities) {
            SocketEntity updated = _player with { TargetX = goalX, TargetY = goalY };
            if (goalX != _player.TargetX || goalY != _player.TargetY) {
                _playerMoveQueue.Enqueue(updated);
            }
            _player = updated;
        }
    }

    public string GetMap() {
        lock (_entities) {
            return _map;
        }
    }

    public long GetMapId() {
        lock (_entities) {
            return _mapId;
        }
    }

    public SocketEntity GetPlayer() {
        lock (_entities) {
            return _player;
        }
    }

    public List<SocketEntity> GetEntities() {
        lock (_entities) {
            return [.._entities.Values];
        }
    }

    public Task Emit(string evt, object data) {
        string json = JsonSerializer.Serialize(data);
        redis.StreamAddAsync($"alo:{evt}", [
            new("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()),
            new("data", json)
        ]);
        return client.EmitSafe(evt, data);
    }

    private readonly Dictionary<string, SocketEntity> _entities = new();
    private string _map = string.Empty;
    private long _mapId = -1;

    private SocketEntity _player;
    private double _playerMoveAccumulator;
    private readonly Queue<SocketEntity> _playerMoveQueue = new();

    private SocketEntity UpdateEntityPosition(SocketEntity entity, double dt) {
        if (!entity.TargetX.HasValue || !entity.TargetY.HasValue) {
            return entity;
        }

        double tx = entity.TargetX.Value;
        double ty = entity.TargetY.Value;

        double dx = tx - entity.X;
        double dy = ty - entity.Y;

        double distance = Math.Sqrt(dx * dx + dy * dy);
        double step = entity.Speed * dt;

        if (step >= distance) {
            return entity with { X = tx, Y = ty, TargetX = null, TargetY = null };
        }

        double dirX = dx / distance;
        double dirY = dy / distance;
        return entity with { X = entity.X + dirX * step, Y = entity.Y + dirY * step };
    }
}
