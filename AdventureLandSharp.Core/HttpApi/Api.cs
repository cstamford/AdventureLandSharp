namespace AdventureLandSharp.Core.HttpApi;

public class Api {
    public Api(ApiConfiguration configuration) {
        _configuration = configuration;
    }

    public async Task<ApiAuthState> LoginAsync() {
        HttpResponseMessage res = await CallApiAsync(_configuration.Address!,
            new SignupOrLoginRequest(_configuration.Credentials.Email, _configuration.Credentials.Password));
        SignupOrLoginResponse body = await ExtractResponse<SignupOrLoginResponse>(res);

        string authToken = res.Headers.GetValues("Set-Cookie").First().Split(';').First().Replace("auth=", "");
        string[] splitToken = authToken.Split('-');
        Debug.Assert(splitToken.Length == 2);

        return new(
            body.Message == "Logged In!",
            splitToken[0],
            splitToken[1]);
    }

    public async Task<ServersAndCharactersResponse> ServersAndCharactersAsync() {
        HttpResponseMessage res = await CallApiAsync(_configuration.Address!, new ServersAndCharactersRequest());
        ServersAndCharactersResponse body = await ExtractResponse<ServersAndCharactersResponse>(res);
        return body;
    }

    public async Task<GameData> FetchGameDataAsync(bool useCache = true) {
        string jsonContent;

        if (useCache && File.Exists("game-data.js")) {
            jsonContent = await File.ReadAllTextAsync("game-data.js");
        } else {
            string a = $"{_configuration.Address!.Address}/data.js";
            HttpResponseMessage response = await _http.GetAsync(a);
            string content = await response.Content.ReadAsStringAsync();
            jsonContent = content[(content.IndexOf('=') + 1)..^2];
            await File.WriteAllTextAsync("game-data.js", jsonContent);
        }

        return JsonSerializer.Deserialize<GameData>(jsonContent);
    }
    private readonly ApiConfiguration _configuration;
    private readonly HttpClient _http = new();

    private async Task<HttpResponseMessage> CallApiAsync<T>(ApiAddress addr, T request) where T: struct {
        string method = request.GetType().GetCustomAttribute<HttpApiMessageAttribute>()!.Name;
        return await _http.PostAsync($"{addr.Address}/api/{method}", new StringContent(
            $"method={Uri.EscapeDataString(method)}&" +
            $"arguments={Uri.EscapeDataString(JsonSerializer.Serialize(request))}",
            Encoding.UTF8,
            "application/x-www-form-urlencoded"
        ));
    }

    private static async Task<T> ExtractResponse<T>(HttpResponseMessage response) where T: struct {
        string responseContent = await response.Content.ReadAsStringAsync();
        JsonElement[] responseElems = JsonSerializer.Deserialize<JsonElement[]>(responseContent)!;
        return JsonSerializer.Deserialize<T>(responseElems.First().GetRawText())!;
    }
}

public readonly record struct ApiAuthState(bool Success, string UserId, string AuthToken);

[AttributeUsage(AttributeTargets.Struct)]
public class HttpApiMessageAttribute(string name) : Attribute {
    public string Name => name;
}
