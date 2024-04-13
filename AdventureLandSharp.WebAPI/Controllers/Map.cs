using AdventureLandSharp.Core;
using Microsoft.AspNetCore.Mvc;

namespace AdventureLandSharp.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class MapController(World world) : ControllerBase
{
    [HttpGet("Connections/{map}", Name = "GetMapConnections")]
    [ProducesResponseType(typeof(IEnumerable<MapConnection>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public IActionResult Connections(string map)
    {
        return world.TryGetMap(map, out var mapObj)
            ? Ok(mapObj.Connections)
            : BadRequest($"Map '{map}' not found.");
    }

    [HttpGet("Data/{map}", Name = "GetMapData")]
    [JsonSettingsName("condensed")]
    [ProducesResponseType(typeof(GameDataMap), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public IActionResult Data(string map)
    {
        return world.TryGetMap(map, out var mapObj)
            ? Ok(mapObj.Data)
            : BadRequest($"Map '{map}' not found.");
    }

    [HttpGet("Geometry/{map}", Name = "GetMapGeometry")]
    [JsonSettingsName("condensed")]
    [ProducesResponseType(typeof(GameLevelGeometry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public IActionResult Geometry(string map)
    {
        return world.TryGetMap(map, out var mapObj)
            ? Ok(mapObj.Geometry)
            : BadRequest($"Map '{map}' not found.");
    }

    [HttpGet("Graph", Name = "GetMapsGraph")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("text/plain")]
    public IActionResult Graph()
    {
        return Ok(MapGraphExporter.Export(world.MapsGraph));
    }

    [HttpGet("List", Name = "GetMapList")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [Produces("application/json")]
    public IActionResult List()
    {
        return Ok(world.Maps.Select(x => x.Key));
    }
}