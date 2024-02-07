using AdventureLandSharp.Socket;
using AdventureLandSharp.Util;
using System.Numerics;

namespace AdventureLandSharp;

public class AdventureLand(GameData game) {
    public GameData Game => game;
    public AStarPath? PlayerPath => _playerPath;

    public void Update(SocketData socket) {
        long mapId = socket.GetMapId();

        if (_mapId != mapId && game.Geometry.TryGetValue(socket.GetMap(), out _level)) {
            _playerGoal = null;
            _playerPath = null;
            _mapId = mapId;
        }

        if (_level is { MinX: 0, MaxX: 0, MinY: 0, MaxY: 0 }) {
            return;
        }

        SocketEntity player = socket.GetPlayer();

        if (_playerGoal != null && _playerPath != null) {
            AStar.GridPos gridStart = Terrain.WorldToGrid(player.X, player.Y, _level);
            AStar.GridPos gridEnd = Terrain.WorldToGrid(_playerGoal.Value, _level);

            if (_playerPath.Path.Count == 0 || _playerPath.End != gridEnd) {
                _playerPath.Update(gridStart, gridEnd);
            } else if (gridStart != gridEnd) {
                if (gridStart == _playerPath.Path[_playerPathIdx]) {
                    _playerPathIdx = Math.Min(_playerPath.Path.Count - 1, _playerPathIdx + 1);
                }
                Vector2 nextGoalWorld = Terrain.GridToWorld(_playerPath.Path[_playerPathIdx], _level);
                socket.UpdatePlayerGoal(nextGoalWorld.X, nextGoalWorld.Y);
            } else {
                _playerGoal = null;
                _playerPath = null;
                _playerPathIdx = 0;
            }
        }
    }

    public void SetGoalPosition(Vector2 position, AStar.MapTerrainCell[,] grid) {
        _playerGoal = position;
        _playerPath = new(grid);
        _playerPathIdx = 0;
    }

    private long _mapId = -1;
    private GameLevelGeometry _level;
    private AStarPath? _playerPath;
    private Vector2? _playerGoal;
    private int _playerPathIdx;
}
