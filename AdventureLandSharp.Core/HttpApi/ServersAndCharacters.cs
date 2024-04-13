namespace AdventureLandSharp.Core.HttpApi;

[HttpApiMessage("servers_and_characters")]
public readonly record struct ServersAndCharactersRequest;

public readonly record struct ServersAndCharactersResponse(
    [property: JsonPropertyName("rewards")]
    List<object> Rewards,
    [property: JsonPropertyName("servers")]
    List<ApiServer> Servers,
    [property: JsonPropertyName("characters")]
    List<ApiCharacter> Characters,
    [property: JsonPropertyName("mail")] int Mail,
    [property: JsonPropertyName("type")] string Type
);

public readonly record struct ApiServer(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("region")] string Region,
    [property: JsonPropertyName("players")]
    int Players,
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("port")] int Port,
    [property: JsonPropertyName("addr")] string Addr
);

public readonly record struct ApiCharacter(
    [property: JsonPropertyName("map")] string Map,
    [property: JsonPropertyName("in")] string In,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("home")] string Home,
    [property: JsonPropertyName("skin")] string Skin,
    [property: JsonPropertyName("cx")] Dictionary<string, string> Cx,
    [property: JsonPropertyName("online")] double Online,
    [property: JsonPropertyName("y")] double Y,
    [property: JsonPropertyName("x")] double X,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("id")] string Id
);