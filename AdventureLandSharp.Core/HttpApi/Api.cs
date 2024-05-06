using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.Core.HttpApi;

public readonly record struct ApiAuthState(bool Success, string UserId,string AuthToken);
public readonly record struct ApiAddress(string Address);
public readonly record struct ApiCredentials(string Email, string Password);

public static class Api {
    public static async Task<ApiAuthState> LoginAsync(ApiAddress addr, ApiCredentials creds) {
        HttpResponseMessage res = await CallApiAsync(addr, new SignupOrLoginRequest(creds.Email, creds.Password));
        SignupOrLoginResponse body = await ExtractResponse<SignupOrLoginResponse>(res);
        Log.Alert($"Login result: {body}");

        string authToken = res.Headers.GetValues("Set-Cookie").First().Split(';').First().Replace("auth=", "");
        string[] splitToken = authToken.Split('-');
        Debug.Assert(splitToken.Length == 2);
    
        return new(
            Success: body.Message == "Logged In!",
            UserId: splitToken[0],
            AuthToken: splitToken[1]);
    }

    public async static Task<ServersAndCharactersResponse> ServersAndCharactersAsync(ApiAddress addr) {
        HttpResponseMessage res = await CallApiAsync(addr, new ServersAndCharactersRequest());
        ServersAndCharactersResponse body = await ExtractResponse<ServersAndCharactersResponse>(res);
        return body;
    }

    public static async Task<GameData> FetchGameDataAsync(ApiAddress addr, bool useCache = true) {
        string jsonContent;

        if (useCache && File.Exists("gamedata.js")) {
            jsonContent = File.ReadAllText("gamedata.js");
        } else {
            HttpResponseMessage response = await _http.GetAsync($"{addr.Address}/data.js");
            string content = await response.Content.ReadAsStringAsync();
            jsonContent = content[(content.IndexOf('=') + 1)..^2];
            File.WriteAllText("gamedata.js", jsonContent);
        }

        return JsonSerializer.Deserialize<GameData>(jsonContent);
    }

    private static readonly HttpClient _http = new();

    private static Task<HttpResponseMessage> CallApiAsync<T>(ApiAddress addr, T request) where T: struct {
        string method = request.GetType().GetCustomAttribute<HttpApiMessageAttribute>()!.Name;
        return _http.PostAsync($"{addr.Address}/api/{method}", new StringContent(
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

[AttributeUsage(AttributeTargets.Struct)]
public class HttpApiMessageAttribute(string name) : Attribute {
    public string Name => name;
}