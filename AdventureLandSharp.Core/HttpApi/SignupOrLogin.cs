namespace AdventureLandSharp.Core.HttpApi;

[HttpApiMessage("signup_or_login")]
public readonly record struct SignupOrLoginRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")]
    string Password,
    [property: JsonPropertyName("only_login")]
    bool OnlyLogin = true
);

public record struct SignupOrLoginResponse(
    [property: JsonPropertyName("message")]
    string Message,
    [property: JsonPropertyName("type")] string Type
);