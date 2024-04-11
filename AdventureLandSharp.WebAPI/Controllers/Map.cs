using AdventureLandSharp.Core;
using Microsoft.AspNetCore.Mvc;

namespace AdventureLandSharp.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class MapController(World world) : ControllerBase {
    [HttpGet("Connections/{map}", Name = "GetMapConnections")]
    [ProducesResponseType(typeof(IEnumerable<MapConnection>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Connections(string map) => world.TryGetMap(map, out Map mapObj)
        ? Ok(mapObj.Connections)
        : BadRequest($"Map '{map}' not found.");

    [HttpGet("Data/{map}", Name = "GetMapData")]
    [JsonSettingsName("condensed")]
    [ProducesResponseType(typeof(GameDataMap), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Data(string map) => world.TryGetMap(map, out Map mapObj)
        ? Ok(mapObj.Data)
        : BadRequest($"Map '{map}' not found.");

    [HttpGet("Geometry/{map}", Name = "GetMapGeometry")]
    [JsonSettingsName("condensed")]
    [ProducesResponseType(typeof(GameLevelGeometry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Geometry(string map) => world.TryGetMap(map, out Map mapObj)
        ? Ok(mapObj.Geometry)
        : BadRequest($"Map '{map}' not found.");

    [HttpGet("List", Name = "GetMapList")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public IActionResult List() => Ok(world.Maps.Select(x => x.Key));
}
