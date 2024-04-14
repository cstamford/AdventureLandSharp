namespace AdventureLandSharp;

public class Bot {
    public Bot(Api api, World world, SocketFactory socketFactory, ILogger<Bot> logger) {
        _api = api;
        _world = world;
        _socketFactory = socketFactory;
        _logger = logger;
    }

    public async Task Run() {
        const string serverRegion = "US";
        const string serverName = "I";

        ApiAuthState auth = await _api.LoginAsync();

        if (!auth.Success) throw new("Failed to login.");

        ServersAndCharactersResponse serversAndCharacters = await _api.ServersAndCharactersAsync();

        ConnectionSettings[] sessionSettings = serversAndCharacters.Characters
            .OrderByDescending(x => x.Level)
            .Select(x => new ConnectionSettings(
                auth.UserId,
                auth.AuthToken,
                serversAndCharacters.Servers.First(x => x is { Region: serverRegion, Name: serverName }),
                x))
            .Take(1)
            .ToArray();


        var sessions = await Task.WhenAll(sessionSettings.Select(async settings => {
            Socket socket = _socketFactory.CreateSocket(settings);
            await socket.Update();
            ICharacter character = CreateCharacterForRun(settings, socket);
            return new { socket, character };
        }).ToArray());

        DateTimeOffset lastTick = DateTimeOffset.UtcNow;
        while (true) {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            float deltaTime = (float)now.Subtract(lastTick).TotalSeconds;
            lastTick = now;

            IEnumerable<Task<bool>> runners = sessions.Select(n => RunOne(n.socket, n.character, deltaTime));

            bool[] results = await Task.WhenAll(runners);
            if (results.Any(n => !n)) return;

            await Task.Delay(250);
        }
    }
    private readonly Api _api;

    private readonly ICharacterFactory _characterFactory = new CharacterFactoryExample();

    private readonly ILogger _logger;
    private readonly SocketFactory _socketFactory;
    private readonly World _world;

    private async Task<bool> RunOne(Socket socket, ICharacter character, float deltaTime) {
        await socket.Update();
        return await character.Update(deltaTime);
    }

    private ICharacter CreateCharacterForRun(ConnectionSettings settings, Socket socket) {
        CharacterClass cls = settings.Character.Type switch {
            "mage" => CharacterClass.Mage,
            "merchant" => CharacterClass.Merchant,
            "paladin" => CharacterClass.Paladin,
            "priest" => CharacterClass.Priest,
            "ranger" => CharacterClass.Ranger,
            "rogue" => CharacterClass.Rogue,
            "warrior" => CharacterClass.Warrior,
            _ => throw new()
        };

        return _characterFactory.Create(cls, _world, socket);
    }
}
