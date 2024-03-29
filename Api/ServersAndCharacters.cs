﻿using System.Text.Json.Serialization;

namespace AdventureLandSharp.Api;

public record struct ServersAndCharacters() : IApiRequest {
    public string Method => "servers_and_characters";
}

public record struct Server(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("region")] string Region,
    [property: JsonPropertyName("players")] int Players,
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("port")] int Port,
    [property: JsonPropertyName("addr")] string Addr);

public record struct Character(
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
    [property: JsonPropertyName("id")] string Id);

public record struct Tutorial(
    [property: JsonPropertyName("progress")] double Progress,
    [property: JsonPropertyName("step")] int Step,
    [property: JsonPropertyName("task")] string Task,
    [property: JsonPropertyName("completed")] List<string> Completed);

public record struct ServersAndCharactersResponse(
    [property: JsonPropertyName("rewards")] List<object> Rewards,
    [property: JsonPropertyName("servers")] List<Server> Servers,
    [property: JsonPropertyName("characters")] List<Character> Characters,
    [property: JsonPropertyName("mail")] int Mail,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("tutorial")] Tutorial Tutorial);
