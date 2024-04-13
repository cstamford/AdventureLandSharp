using AdventureLandSharp.Core;
using AdventureLandSharp.Core.HttpApi;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Interfaces;

namespace AdventureLandSharp.Example;

// Implements a session coordinator that connects to the server with the highest level character.
// It will change the selected character every minute, cycling through the list of characters.
public class BasicSessionCoordinator(
    World world,
    ApiAuthState authState,
    ServersAndCharactersResponse serverState) : ISessionCoordinator
{
    public IEnumerable<SessionPlan> Plans => [NextPlan()];

    private DateTimeOffset _lastCharacterChange = DateTimeOffset.UtcNow;
    private static readonly TimeSpan _characterChangeInterval = TimeSpan.FromMinutes(1);
    private int _characterIdx = 0;

    private SessionPlan NextPlan() => new(
        Connection: NextCharacter(),
        SessionFactory: (world, settings, characterFactory) => new BasicSession(world, settings, characterFactory, withGui: true),
        CharacterFactory: (world, settings, cls) => new BasicCharacter(world, settings, cls)
    );

    private ConnectionSettings NextCharacter() {
        const string serverRegion = "US";
        const string serverName = "III";

        List<ConnectionSettings> characters = serverState.Characters
            .OrderByDescending(x => x.Level)
            .Select(x => new ConnectionSettings(
                UserId: authState.UserId,
                AuthToken: authState.AuthToken,
                Server: serverState.Servers.First(x => x is { Region: serverRegion, Name: serverName}),
                Character: x))
            .ToList();

        if (DateTimeOffset.UtcNow.Subtract(_lastCharacterChange) >= _characterChangeInterval) {
            ++_characterIdx;

            if (++_characterIdx >= characters.Count) {
                _characterIdx = 0;
            }

            _lastCharacterChange = DateTimeOffset.UtcNow;
        }

        return characters[_characterIdx];
    }

    [SessionCoordinatorFactory]
    public static ISessionCoordinator Create(World world, ApiAuthState apiAuthState, ServersAndCharactersResponse api)
        => new BasicSessionCoordinator(world, apiAuthState, api);
}
