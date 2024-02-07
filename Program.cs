using AdventureLandSharp;
using AdventureLandSharp.Api;
using AdventureLandSharp.Socket;
using AdventureLandSharp.Util;
using StackExchange.Redis;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using HttpClient http = new();

ConnectionMultiplexer redis = ConnectionMultiplexer.Connect($"{Credentials.RedisIp},password={Credentials.RedisPassword}");
IDatabase redisDb = redis.GetDatabase();

// Game data. Grab it from the static URL.
GameData gameData = await FetchGameData();
Task gridGenerationTask = Terrain.GenerateTerrainGrids(gameData);

// HTTP login flow.
// 1. Call signup_or_login with credentials. Response contains a cookie header with important tokens.
// 2. Call servers_and_characters, and select a server from the list.

AuthState auth = await Login();

if (!auth.Success) {
    throw new Exception("Failed to login.");
}

ServersAndCharactersResponse data = (await CallApi<ServersAndCharactersResponse>(new ServersAndCharacters())).Result;
Server server = data.Servers.Where(x => x.Name != "PVP" && x.Region is "EU" or "US").MinBy(x => x.Players);
Character character = data.Characters.First();

// Socket login flow.
// 1. Wait for "welcome" from server.
// 2. Emit "loaded".
// 3. Wait for "entities" from server.
// 4. Emit "auth".
// 5. Wait for "start" from server.

bool authenticated = false;
bool started = false;

SocketIOClient.SocketIO socketClient = new($"https://{server.Addr}:{server.Port}");
SocketData socketData = new(socketClient, redisDb);

socketClient.OnAny((evt, e) => {
    redisDb.StreamAddAsync($"al:{evt}", [
        new("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()),
        new("data", e.ToString())
    ]);
});

// Note: welcome will give us server and map info, but this is for the spectator.
// It has nothing to do with our characters or location, so we just ignore it.
socketClient.OnSafe("welcome", async e => {
    socketData.UpdateFrom(e.GetValue<ServerToClient.Welcome>());
    await socketData.Emit("loaded", new ClientToServer.Loaded(
        Success: true));
});

// Basically contains a snapshot of the game state.
socketClient.OnSafe("entities", async e => {
    if (authenticated && started) {
        socketData.UpdateFrom(e.GetValue<ServerToClient.Entities>());
    } else if (!authenticated) {
        await socketData.Emit("auth", new ClientToServer.Auth(
            UserId: auth.UserId,
            CharacterId: character.Id,
            AuthToken: auth.AuthToken));

        authenticated = true;
    }
});

// Identical to player, but called once at the very start.
socketClient.OnSafe("start", e => {
    socketData.UpdateFrom(e.GetValue<ServerToClientTypes.Player>());
    started = true;
});

// Messages: We just log the messages to console (for now).
socketClient.OnSafe("chat_log", e => Console.WriteLine($"chat_log: {e}"));
socketClient.OnSafe("disconnect", e => Console.WriteLine($"disconnect: {e}"));
socketClient.OnSafe("disconnect_reason", e => Console.WriteLine($"disconnect_reason: {e}"));
socketClient.OnSafe("game_error", e => Console.WriteLine($"game_error: {e}"));
socketClient.OnSafe("pm", e => Console.WriteLine($"pm: {e}"));
socketClient.OnSafe("server_message", e => Console.WriteLine($"server_message: {e}"));

// Pipe through any other interesting events.
socketClient.OnSafe("correction", e => socketData.UpdateFrom(e.GetValue<ServerToClient.Correction>()));
socketClient.OnSafe("death", e => socketData.UpdateFrom(e.GetValue<ServerToClient.Death>()));
socketClient.OnSafe("disappear", e => socketData.UpdateFrom(e.GetValue<ServerToClient.Disappear>()));
socketClient.OnSafe("player", e => socketData.UpdateFrom(e.GetValue<ServerToClientTypes.Player>()));

// Connect to the server, blocking until the entire auth process is complete.
// TODO: We need to handle failures. Probably something with throw on any of the bad events, wait 30, reconnect.
await socketClient.ConnectAsync();
while (!started) {
    Thread.Sleep(1);
}

// Wait for the A* grids to complete generating.
// TODO: We can refactor so we only wait for our current level, which might save a bit of time on startup.
await gridGenerationTask;

AdventureLand al = new(gameData);
using AdventureLandRenderer alRenderer = new(al);

DateTime lastUpdate = DateTime.UtcNow;

while (true) {
    DateTime now = DateTime.UtcNow;
    TimeSpan elapsedTime = now - lastUpdate;
    lastUpdate = now;

    socketData.Update((float)elapsedTime.TotalSeconds);
    al.Update(socketData);
    alRenderer.Update(socketData);
    Thread.Sleep(1);
}

async Task<AuthState> Login() {
    ApiResponse<SignupOrLoginResponse> res = await CallApi<SignupOrLoginResponse>(new SignupOrLogin(Credentials.Email, Credentials.Password));

    string authToken = res.Headers.GetValues("Set-Cookie")
        .First()
        .Split(';')
        .First()
        .Replace("auth=", "");

    string[] splitToken = authToken.Split('-');
    Debug.Assert(splitToken.Length == 2);

    return new(
        Success: res.Result.Message == "Logged In!",
        UserId: splitToken[0],
        AuthToken: splitToken[1]);
}

async Task<ApiResponse<T>> CallApi<T>(IApiRequest request) {
    HttpResponseMessage response = await http.PostAsync($"https://adventure.land/api/{request.Method}", new StringContent(
        $"method={Uri.EscapeDataString(request.Method)}&" +
        $"arguments={Uri.EscapeDataString(JsonSerializer.Serialize((object)request))}",
        Encoding.UTF8,
        "application/x-www-form-urlencoded"
    ));

    string responseContent = await response.Content.ReadAsStringAsync();
    JsonElement[] responseElems = JsonSerializer.Deserialize<JsonElement[]>(responseContent)!;

    return new(
        JsonSerializer.Deserialize<T>(responseElems.First().GetRawText())!,
        response.Headers);
}

async Task<GameData> FetchGameData() {
    string jsonContent;

    // TODO: Version check based on welcome message. Reload data if our cached version is old.
    if (File.Exists("gamedata.js")) {
        jsonContent = File.ReadAllText("gamedata.js");
    } else {
        HttpResponseMessage response = await http.GetAsync("https://adventure.land/data.js");
        string content = await response.Content.ReadAsStringAsync();
        jsonContent = content[(content.IndexOf('=') + 1)..^2];
        File.WriteAllText("gamedata.js", jsonContent);
    }

    return JsonSerializer.Deserialize<GameData>(jsonContent);
}

record struct ApiResponse<T>(
    T Result,
    HttpResponseHeaders Headers);

record struct AuthState(
    bool Success,
    string UserId,
    string AuthToken);
