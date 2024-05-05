using System.Collections.Concurrent;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.HttpApi;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Interfaces;

namespace AdventureLandSharp.Example;

// Implements an example session coordinator that connects to the server with three random characters. It will change the selected characters every minute.
public class BasicSessionCoordinator(
    World world,
    ApiAuthState authState,
    ServersAndCharactersResponse serverState) : ISessionCoordinator
{
    [SessionCoordinatorFactory]
    public static ISessionCoordinator Create(World world, ApiAuthState apiAuthState, ServersAndCharactersResponse api)
        => new BasicSessionCoordinator(world, apiAuthState, api);

    public IEnumerable<SessionPlan> Plans => GetPlans();

    private IEnumerable<SessionPlan> _plans = [];
    private DateTimeOffset _nextPlanRefresh = DateTimeOffset.UtcNow;
    private readonly ConcurrentDictionary<string, ICharacter> _characters = [];

    private IEnumerable<SessionPlan> GetPlans() {
        if (DateTimeOffset.UtcNow >= _nextPlanRefresh) {
            ConnectionSettings[] characters = [..serverState.Characters
                .OrderByDescending(x => x.Level)
                .Select(x => new ConnectionSettings(
                    UserId: authState.UserId,
                    AuthToken: authState.AuthToken,
                    Server: serverState.Servers.First(x => x is { Region: "US", Name: "III"}),
                    Character: x))];

            for (int i = 0; i < characters.Length; ++i) {
                int j = Random.Shared.Next(i, characters.Length);
                (characters[j], characters[i]) = (characters[i], characters[j]);
            }

            ApiCharacter guiChar = characters[0].Character;

            _plans = characters.Take(3).Select(x => new SessionPlan(
                Connection: x,
                SessionFactory: (w, s, c, g) => new BasicSession(w, s, c, g),
                CharacterFactory: (w, s, c) => {
                    ICharacter character = new BasicCharacter(w, s, c);
                    _characters[s.Player.Id] = character;
                    return character;
                },
                GuiFactory: (guiChar == x.Character) ? (w, c) => new BasicCharacterGui(w, c) : null)
            );

            _nextPlanRefresh = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5));
        }

        return _plans;
    }
}
