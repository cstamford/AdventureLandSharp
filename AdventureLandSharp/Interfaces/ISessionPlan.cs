using AdventureLandSharp.Core;
using AdventureLandSharp.Core.HttpApi;
using AdventureLandSharp.Core.SocketApi;

namespace AdventureLandSharp.Interfaces;

public readonly record struct SessionPlan(
    ConnectionSettings Connection,
    SessionFactory SessionFactory,
    CharacterFactory CharacterFactory
);

public interface ISessionCoordinator {
    public IEnumerable<SessionPlan> Plans { get; }
}

public delegate ISessionCoordinator SessionCoordinatorFactory(
    World world,
    ApiAuthState authState,
    ServersAndCharactersResponse serversAndCharacters
);
