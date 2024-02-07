using System.Text.Json.Serialization;

namespace AdventureLandSharp.Socket;

public static class ClientToServer {
    public const long Width = 3840;
    public const long Height = 2160;
    public const long Scale = 2;

    public record struct Auth(
        [property: JsonPropertyName("user")] string UserId,
        [property: JsonPropertyName("character")] string CharacterId,
        [property: JsonPropertyName("auth")] string AuthToken,
        [property: JsonPropertyName("width")] long Width = Width,
        [property: JsonPropertyName("height")] long Height = Height,
        [property: JsonPropertyName("scale")] long Scale = Scale,
        [property: JsonPropertyName("no_html")] bool NoHtml = false,
        [property: JsonPropertyName("no_graphics")] bool NoGraphics = false
    );

    public record struct Move(
        [property: JsonPropertyName("x")] double X,
        [property: JsonPropertyName("y")] double Y,
        [property: JsonPropertyName("going_x")] double TargetX,
        [property: JsonPropertyName("going_y")] double TargetY,
        [property: JsonPropertyName("m")] long MapId
    );

    public record struct Loaded(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("width")] long Width = Width,
        [property: JsonPropertyName("height")] long Height = Height,
        [property: JsonPropertyName("scale")] long Scale = Scale
    );
}
