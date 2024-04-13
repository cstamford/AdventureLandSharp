namespace AdventureLandSharp.Core.HttpApi;

public class Api
{
    private readonly ApiConfiguration _configuration;
    private readonly HttpClient _http = new();

    public Api(ApiConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ApiAuthState> LoginAsync()
    {
        var res = await CallApiAsync(_configuration.Address!,
            new SignupOrLoginRequest(_configuration.Credentials.Email, _configuration.Credentials.Password));
        var body = await ExtractResponse<SignupOrLoginResponse>(res);

        var authToken = res.Headers.GetValues("Set-Cookie").First().Split(';').First().Replace("auth=", "");
        var splitToken = authToken.Split('-');
        Debug.Assert(splitToken.Length == 2);

        return new ApiAuthState(
            body.Message == "Logged In!",
            splitToken[0],
            splitToken[1]);
    }

    public async Task<ServersAndCharactersResponse> ServersAndCharactersAsync()
    {
        var res = await CallApiAsync(_configuration.Address!, new ServersAndCharactersRequest());
        var body = await ExtractResponse<ServersAndCharactersResponse>(res);
        return body;
    }

    public async Task<GameData> FetchGameDataAsync(bool useCache = true)
    {
        string jsonContent;

        if (useCache && File.Exists("game-data.js"))
        {
            jsonContent = await File.ReadAllTextAsync("game-data.js");
        }
        else
        {
            var a = $"{_configuration.Address!.Address}/data.js";
            var response = await _http.GetAsync(a);
            var content = await response.Content.ReadAsStringAsync();
            jsonContent = content[(content.IndexOf('=') + 1)..^2];
            await File.WriteAllTextAsync("game-data.js", jsonContent);
        }

        return JsonSerializer.Deserialize<GameData>(jsonContent);
    }

    private async Task<HttpResponseMessage> CallApiAsync<T>(ApiAddress addr, T request) where T : struct
    {
        var method = request.GetType().GetCustomAttribute<HttpApiMessageAttribute>()!.Name;
        return await _http.PostAsync($"{addr.Address}/api/{method}", new StringContent(
            $"method={Uri.EscapeDataString(method)}&" +
            $"arguments={Uri.EscapeDataString(JsonSerializer.Serialize(request))}",
            Encoding.UTF8,
            "application/x-www-form-urlencoded"
        ));
    }

    private static async Task<T> ExtractResponse<T>(HttpResponseMessage response) where T : struct
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseElems = JsonSerializer.Deserialize<JsonElement[]>(responseContent)!;
        return JsonSerializer.Deserialize<T>(responseElems.First().GetRawText())!;
    }
}

public readonly record struct ApiAuthState(bool Success, string UserId, string AuthToken);

[AttributeUsage(AttributeTargets.Struct)]
public class HttpApiMessageAttribute(string name) : Attribute
{
    public string Name => name;
}