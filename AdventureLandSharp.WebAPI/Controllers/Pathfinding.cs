using System.Numerics;
using System.Text.Json.Serialization;
using AdventureLandSharp.Core;
using Microsoft.AspNetCore.Mvc;

namespace AdventureLandSharp.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class PathfindingController(World world) : ControllerBase
{
    [HttpGet("Grid/{map}", Name = "GetPathfindingGrid")]
    [JsonSettingsName("condensed")]
    [ProducesResponseType(typeof(GridResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Grid(string map)
    {
        if (world.TryGetMap(map, out var mapObj))
        {
            var grid = mapObj.Grid;

            List<bool> walkable = [];
            List<float> cost = [];

            for (var y = 0; y < grid.Height; ++y)
            for (var x = 0; x < grid.Width; ++x)
            {
                MapGridCell cell = new(x, y);
                walkable.Add(grid.IsWalkable(cell));
                cost.Add(grid.Cost(cell));
            }

            return Ok(new GridResponse(MapGrid.CellSize, grid.Width, grid.Height, walkable, cost));
        }

        return BadRequest($"Map '{map}' not found.");
    }

    [HttpPost("Path", Name = "GetPathfindingPath")]
    [JsonSettingsName("condensed")]
    [ProducesResponseType(typeof(IEnumerable<IPathResponseStep>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Path(PathRequest req)
    {
        if (world.TryGetMap(req.Source.Map, out var sourceMap) && world.TryGetMap(req.Dest.Map, out var destMap))
        {
            var startLocation = sourceMap.Grid.WorldToGrid(req.Source.Location);
            var endLocation = destMap.Grid.WorldToGrid(req.Dest.Location);

            MapLocation start = new(sourceMap, req.Source.Location);
            MapLocation end = new(destMap, req.Dest.Location);

            var path = world.FindRoute(start, end, req.Heuristic);

            return req.HumanReadable
                ? Ok(path.Select(x => x.ToString()))
                : Ok(path.Select<IMapGraphEdge, IPathResponseStep>(x => x switch
                {
                    MapGraphEdgeInterMap edge => new PathResponseInterMapStep(
                        new PathLocation(edge.Source.Map.Name, edge.Source.Location),
                        new PathLocation(edge.Dest.Map.Name, edge.Dest.Location),
                        edge.Type
                    ),
                    MapGraphEdgeIntraMap edge => new PathResponseIntraMapStep(
                        new PathLocation(edge.Source.Map.Name, edge.Source.Location),
                        new PathLocation(edge.Dest.Map.Name, edge.Dest.Location),
                        [.. edge.Path]
                    ),
                    MapGraphEdgeTeleport edge => new PathResponseTeleportStep(
                        new PathLocation(edge.Source.Map.Name, edge.Source.Location),
                        new PathLocation(edge.Dest.Map.Name, edge.Dest.Location)
                    ),
                    _ => throw new NotImplementedException()
                }));
        }

        return BadRequest($"Maps '{req.Source.Map}' or '{req.Dest.Map}' not found.");
    }

    public readonly record struct GridResponse(
        int CellSize,
        int Width,
        int Height,
        IEnumerable<bool> TerrainWalkableXY,
        IEnumerable<float> TerrainCostXY
    );

    public readonly record struct PathLocation(string Map, Vector2 Location);

    public readonly record struct PathRequest(
        PathLocation Source,
        PathLocation Dest,
        MapGridHeuristic Heuristic = MapGridHeuristic.Diagonal,
        bool HumanReadable = false
    );

    [JsonDerivedType(typeof(PathResponseInterMapStep))]
    [JsonDerivedType(typeof(PathResponseIntraMapStep))]
    [JsonDerivedType(typeof(PathResponseTeleportStep))]
    public interface IPathResponseStep
    {
        public PathLocation Source { get; }
        public PathLocation Dest { get; }
    }

    public readonly record struct PathResponseInterMapStep(
        PathLocation Source,
        PathLocation Dest,
        MapConnectionType Type) : IPathResponseStep;

    public readonly record struct PathResponseIntraMapStep(PathLocation Source, PathLocation Dest, Vector2[] Path)
        : IPathResponseStep;

    public readonly record struct PathResponseTeleportStep(PathLocation Source, PathLocation Dest, bool Teleport = true)
        : IPathResponseStep;
}