using System.Text.Json.Serialization;

namespace AdventureLandSharp.Api;

public record struct SignupOrLogin(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("only_login")] bool OnlyLogin = true) : IApiRequest {
    public string Method => "signup_or_login";
}

public record struct SignupOrLoginResponse(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("type")] string Type);
