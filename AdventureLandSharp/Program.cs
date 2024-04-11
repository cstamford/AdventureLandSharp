using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.HttpApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp;

using HttpClient http = new();

const string serverAddr = "localhost:8083";
const string user = "dev";
const string pass = "dev";
const string serverRegion = "US";
const string serverName = "III";

ApiAddress apiAddr = new($"http://{serverAddr}");
GameData data = await Api.FetchGameDataAsync(apiAddr);
World world = new(data);

// HTTP login flow.
// 1. Call signup_or_login with credentials. Response contains a cookie header with important tokens.
// 2. Call servers_and_characters, and select a server from the list.
ApiCredentials creds = new(user, pass);
ApiAuthState auth = await Api.LoginAsync(apiAddr, creds);

if (!auth.Success) {
    throw new Exception("Failed to login.");
}

ServersAndCharactersResponse serversAndCharacters = await Api.ServersAndCharactersAsync(apiAddr);
ConnectionSettings settings = new(
    UserId: auth.UserId,
    AuthToken: auth.AuthToken,
    Server: serversAndCharacters.Servers.First(x => x is { Region: serverRegion, Name: serverName}),
    Character: serversAndCharacters.Characters.MaxBy(x => x.Level));

while (true) {
    Socket socket = new(
        data,
        settings, 
        (evt, data) => Log.Debug($"[SEND] Event: {evt}, Data: {data}"),
        (evt, data) => Log.Debug($"[RECV] Event: {evt}, Data: {data}"));

    while (!socket.Connected) {
        socket.Update();
        Thread.Yield();
    }

    DebugGui gui = new(world, socket);

    IEnumerable<IMapGraphEdge> pathToMain = world.FindRoute(
        new(world.GetMap(socket.Player.Map), socket.Player.Position),
        new(world.GetMap("halloween"), new(8, 630)));

    PlayerGraphTraversal traversal = new(socket, new(pathToMain));

    while (socket.Connected) {
        socket.Update();
        traversal.Update();
        gui.Update();
        Thread.Yield();
    }
}
