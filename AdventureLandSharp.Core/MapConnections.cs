namespace AdventureLandSharp.Core;

public enum MapConnectionType
{
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

public class MapConnections
{
    private readonly List<MapConnection> _connections;

    public MapConnections(string mapName, ref readonly GameData gameData, ref readonly GameDataMap mapData)
    {
        _connections = CreateConnections(mapName, in gameData, in mapData);
    }

    public IReadOnlyList<MapConnection> Connections => _connections;

    private static List<MapConnection> CreateConnections(string mapName, ref readonly GameData gameData,
        ref readonly GameDataMap mapData)
    {
        List<MapConnection> connections = [];

        foreach (var door in mapData.Doors)
        {
            var destMap = door[4].GetString()!;

            if (door.Any(x =>
                    x.ValueKind == JsonValueKind.String &&
                    x.GetString()!.Contains("locked"))) continue; // Note: We skip locked doors.

            if (!gameData.Maps.TryGetValue(destMap, out var destination)) continue;

            var destSpawnId = door[5].GetInt64();
            var destinationPosition = destination.SpawnPositions[destSpawnId];
            connections.Add(new MapConnection(
                MapConnectionType.Door,
                mapName, (float) door[0].GetDouble(), (float) door[1].GetDouble(),
                destMap, (float) destinationPosition[0], (float) destinationPosition[1], destSpawnId
            ));
        }

        var transporterNpc = gameData.Npcs["transporter"];
        foreach (var npc in mapData.Npcs.Where(npc => npc.Id == "transporter"))
        foreach (var (destMap, destSpawnId) in transporterNpc.Places!)
        {
            var destinationPosition = gameData.Maps[destMap].SpawnPositions[destSpawnId];
            connections.Add(new MapConnection(
                MapConnectionType.Transporter,
                mapName, (float) npc.Position[0], (float) npc.Position[1],
                destMap, (float) destinationPosition[0], (float) destinationPosition[1], destSpawnId
            ));
        }

        var jailSpawn = gameData.Maps["jail"].SpawnPositions[0];
        var mainSpawn = gameData.Maps["main"].SpawnPositions[0];

        connections.Add(new MapConnection(
            MapConnectionType.Leave,
            "jail", (float) jailSpawn[0], (float) jailSpawn[1],
            "main", (float) mainSpawn[0], (float) mainSpawn[1], 0
        ));

        return connections;
    }
}