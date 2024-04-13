using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.HttpApi;
using AdventureLandSharp.Game;
using AdventureLandSharp;

const string serverAddrLocal = "localhost:8083";
const string serverAddrLive = "adventure.land";
const string user = "dev";
const string pass = "dev";
const string serverRegion = "US";
const string serverName = "III";

ApiAddress apiAddr = new($"http://{serverAddrLocal}");
GameData data = await Api.FetchGameDataAsync(apiAddr);
World world = new(data);

ApiCredentials creds = new(user, pass);
ApiAuthState auth = await Api.LoginAsync(apiAddr, creds);

if (!auth.Success) {
    throw new Exception("Failed to login.");
}

ServersAndCharactersResponse serversAndCharacters = await Api.ServersAndCharactersAsync(apiAddr);

List<ConnectionSettings> sessionSettings = serversAndCharacters.Characters
    .OrderByDescending(x => x.Level)
    .Select(x => new ConnectionSettings(
        UserId: auth.UserId,
        AuthToken: auth.AuthToken,
        Server: serversAndCharacters.Servers.First(x => x is { Region: serverRegion, Name: serverName}),
        Character: x))
    .Take(1)
    .ToList();

List<Task> sessions = [..Enumerable.Range(0, sessionSettings.Count).Select(_ => Task.CompletedTask)];

ICharacterFactory exampleCharacterFactory = new CharacterFactoryExample();

while (true) {
    for (int i = 0; i < sessionSettings.Count; i++) {
        ConnectionSettings settings = sessionSettings[i];
        Task sessionTask = sessions[i];
        bool isGuiSession = i == 0;

        if (sessionTask.IsCompleted) {
            sessions[i] = Task.Run(() => {
                using Session session = new(world, settings, exampleCharacterFactory, withGui: isGuiSession);
                session.EnterUpdateLoop();
            });

            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }

    await Task.WhenAny(sessions);
}
