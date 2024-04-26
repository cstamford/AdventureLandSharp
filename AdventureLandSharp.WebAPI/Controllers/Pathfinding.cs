using System.Numerics;
using System.Text.Json.Serialization;
using AdventureLandSharp.Core;
using Microsoft.AspNetCore.Mvc;

namespace AdventureLandSharp.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class PathfindingController(World world) : ControllerBase {
    public readonly record struct GridResponse(
        int CellSize,
        int Width,
        int Height,
        IEnumerable<bool> TerrainWalkableXY,
        IEnumerable<float> TerrainCostXY
    );

    [HttpGet("Grid/{map}", Name = "GetPathfindingGrid")]
    [JsonSettingsName("condensed")]
    [ProducesResponseType(typeof(GridResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Grid(string map) {
        if (world.TryGetMap(map, out Map mapObj)) {
            MapGrid grid = mapObj.Grid;

            List<bool> walkable = [];
            List<float> cost = [];

            for (int y = 0; y < grid.Height; ++y) {
                for (int x = 0; x < grid.Width; ++x) {
                    MapGridCell cell = new(x, y);
                    walkable.Add(cell.IsWalkable(grid));
                    cost.Add(cell.Cost(grid));
                }
            }

            return Ok(new GridResponse(MapGrid.CellSize, grid.Width, grid.Height, walkable, cost));
        }
        return BadRequest($"Map '{map}' not found.");
    }

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
    public interface IPathResponseStep { 
        public PathLocation Source { get; }
        public PathLocation Dest { get; }
    }

    public readonly record struct PathResponseInterMapStep(PathLocation Source, PathLocation Dest, MapConnectionType Type) : IPathResponseStep;
    public readonly record struct PathResponseIntraMapStep(PathLocation Source, PathLocation Dest, Vector2[] Path) : IPathResponseStep;
    public readonly record struct PathResponseTeleportStep(PathLocation Source, PathLocation Dest, bool Teleport = true) : IPathResponseStep;

    [HttpPost("Path", Name = "GetPathfindingPath")]
    [JsonSettingsName("condensed")]
    [ProducesResponseType(typeof(IEnumerable<IPathResponseStep>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Path(PathRequest req) {
        if (world.TryGetMap(req.Source.Map, out Map sourceMap) && world.TryGetMap(req.Dest.Map, out Map destMap)) {
            MapGridCell startLocation = req.Source.Location.Grid(sourceMap);
            MapGridCell endLocation = req.Dest.Location.Grid(destMap);

            MapLocation start = new(sourceMap, req.Source.Location);
            MapLocation end = new(destMap, req.Dest.Location);

            IEnumerable<IMapGraphEdge> path = world.FindRoute(start, end, new(Heuristic: req.Heuristic));

            return req.HumanReadable ?
                Ok(path.Select(x => x.ToString())) :
                Ok(path.Select<IMapGraphEdge, IPathResponseStep>(x => x switch {
                    MapGraphEdgeInterMap edge => new PathResponseInterMapStep(
                        new(edge.Source.Map.Name, edge.Source.Location),
                        new(edge.Dest.Map.Name, edge.Dest.Location),
                        edge.Type
                    ),
                    MapGraphEdgeIntraMap edge => new PathResponseIntraMapStep(
                        new(edge.Source.Map.Name, edge.Source.Location),
                        new(edge.Dest.Map.Name, edge.Dest.Location),
                        [.. edge.Path]
                    ),
                    MapGraphEdgeTeleport edge => new PathResponseTeleportStep(
                        new(edge.Source.Map.Name, edge.Source.Location),
                        new(edge.Dest.Map.Name, edge.Dest.Location)
                    ),
                    _ => throw new NotImplementedException()
                }));
        }

        return BadRequest($"Maps '{req.Source.Map}' or '{req.Dest.Map}' not found.");
    }
}
