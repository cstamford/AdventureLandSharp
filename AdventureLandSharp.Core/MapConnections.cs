using System.Diagnostics;
using System.Text.Json;

namespace AdventureLandSharp.Core;

public enum MapConnectionType {
    Door,
    Transporter,
    Leave
}

public readonly record struct MapConnection(
    MapConnectionType Type,
    string SourceMap,
    float SourceX,
    float SourceY,
    string DestMap,
    float DestX,
    float DestY,
    long DestSpawnId
);

public class MapConnections(string mapName, GameData gameData, GameDataMap mapData) {
    public IReadOnlyList<MapConnection> Connections => _connections;

    private readonly List<MapConnection> _connections = CreateConnections(mapName, gameData, mapData);

    private static List<MapConnection> CreateConnections(string mapName, GameData gameData, GameDataMap mapData) => [
        .. GetDoorConnections(mapName, gameData, mapData),
        .. GetTransporterConnections(mapName, gameData, mapData),
        GetJailConnection(gameData)
    ];

    private static List<MapConnection> GetDoorConnections(string mapName, GameData gameData, GameDataMap mapData) {
        List<MapConnection> connections = [];

        foreach (JsonElement[] door in mapData.Doors) {
            string destMap = door[4].GetString()!;

            if (door.Any(x => x.ValueKind == JsonValueKind.String && x.GetString()!.Contains("locked")) && destMap != "bank_b") {
                continue; // Note: We skip locked doors.
            }

            if (gameData.Maps.TryGetValue(destMap, out GameDataMap destination)) {
                long sourceSpawnId = door[6].GetInt64();
                long destSpawnId = door[5].GetInt64();
                double[] sourcePosition = mapData.SpawnPositions[sourceSpawnId];
                double[] destinationPosition = destination.SpawnPositions[destSpawnId];
                connections.Add(new(
                    MapConnectionType.Door,
                    mapName, (float)sourcePosition[0], (float)sourcePosition[1],
                    destMap, (float)destinationPosition[0], (float)destinationPosition[1], destSpawnId
                ));
            }
        }

        return connections;
    }

    private static List<MapConnection> GetTransporterConnections(string mapName, GameData gameData, GameDataMap mapData) {
        List<MapConnection> connections = [];

        GameDataNpc transporterNpc = gameData.Npcs["transporter"];
        foreach (GameDataMapNpc npc in mapData.Npcs.Where(npc => npc.Id == "transporter")) {
            foreach ((string? destMap, long destSpawnId) in transporterNpc.Places!) {
                Debug.Assert(npc.Position != null);
                double[] destinationPosition = gameData.Maps[destMap].SpawnPositions[destSpawnId];
                connections.Add(new(
                    MapConnectionType.Transporter,
                    mapName, (float)npc.Position[0], (float)npc.Position[1],
                    destMap, (float)destinationPosition[0], (float)destinationPosition[1], destSpawnId
                ));
            }
        }

        return connections;
    }

    private static MapConnection GetJailConnection(GameData gameData) {
        double[] jailSpawn = gameData.Maps["jail"].SpawnPositions[0];
        double[] mainSpawn = gameData.Maps["main"].SpawnPositions[0];

        return new(
            MapConnectionType.Leave,
            "jail", (float)jailSpawn[0], (float)jailSpawn[1],
            "main", (float)mainSpawn[0], (float)mainSpawn[1], 0
        );
    }
}
