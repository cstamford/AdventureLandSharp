using AdventureLandSharp.Core;
using AdventureLandSharp.Core.HttpApi;
using AdventureLandSharp.Interfaces;

const string serverAddrLocal = "localhost:8083";
const string serverAddrLive = "adventure.land";
const string user = "dev";
const string pass = "dev";

ApiAddress apiAddr = new($"http://{serverAddrLocal}");
GameData data = await Api.FetchGameDataAsync(apiAddr);
World world = new(data);

ApiCredentials creds = new(user, pass);
ApiAuthState auth = await Api.LoginAsync(apiAddr, creds);

if (!auth.Success) {
    throw new Exception("Failed to login.");
}

ServersAndCharactersResponse serversAndCharacters = await Api.ServersAndCharactersAsync(apiAddr);
ISessionCoordinator coordinator = DependencyResolver.SessionCoordinator()(world, auth, serversAndCharacters);
List<RunningSession> sessions = [];

while (true) {
    IEnumerable<RunningSession> staleSessions = sessions.Where(x => 
        x.SessionTask.IsCompleted ||
        !coordinator.Plans.Any(y => x.Session.Settings == y.Connection));

    foreach (RunningSession staleSession in staleSessions) {
        staleSession.Session.Dispose();
        await staleSession.SessionTask;
    }

    sessions.RemoveAll(x => staleSessions.Contains(x));

    IEnumerable<SessionPlan> freshSessions = coordinator.Plans
        .Where(x => !sessions.Any(y => y.Session.Settings == x.Connection));

    foreach (SessionPlan plan in freshSessions) {
        ISession session = plan.SessionFactory(world, plan.Connection, plan.CharacterFactory);
        Task sessionTask = Task.Run(session.EnterUpdateLoop);
        sessions.Add(new RunningSession(session, sessionTask));
    }

    await Task.WhenAny([
        ..sessions.Select(x => x.SessionTask),
        Task.Delay(TimeSpan.FromMilliseconds(100))
    ]);
}

internal readonly record struct RunningSession(ISession Session, Task SessionTask);
