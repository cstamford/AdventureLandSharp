using AdventureLandSharp.Core;
using AdventureLandSharp.Core.HttpApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Interfaces;

const string user = "dev";
const string pass = "dev";

GameData data = await Api.FetchGameDataAsync(Utils.ApiAddress);
World world = new(data);

ApiCredentials creds = new(user, pass);
ApiAuthState auth = await Api.LoginAsync(Utils.ApiAddress, creds);

if (!auth.Success) {
    throw new Exception("Failed to login.");
}

ServersAndCharactersResponse serversAndCharacters = await Api.ServersAndCharactersAsync(Utils.ApiAddress);
ISessionCoordinator coordinator = DependencyResolver.SessionCoordinator()(world, auth, serversAndCharacters);
List<RunningSession> sessions = [];

while (true) {
    IEnumerable<SessionPlan> plans = coordinator.Plans;
    IEnumerable<RunningSession> staleSessions = sessions.Where(x => 
        x.SessionTask.IsCompleted || 
        !plans.Any(y => x.Session.Settings == y.Connection));

    foreach (RunningSession staleSession in staleSessions) {
        staleSession.Session.Dispose();
        await staleSession.SessionTask;
    }

    sessions.RemoveAll(x => staleSessions.Contains(x));

    IEnumerable<SessionPlan> freshSessions = plans.Where(x => 
        !sessions.Any(y => y.Session.Settings == x.Connection));

    foreach (SessionPlan plan in freshSessions) {
        ISession session = plan.SessionFactory(
            world,
            plan.Connection,
            plan.CharacterFactory,
            plan.GuiFactory
        );

        sessions.Add(new RunningSession(session, Task.Run(() => {
            string? oldThreadName = Thread.CurrentThread.Name;

            try {
                Thread.CurrentThread.Name = $"{plan.Connection.Character.Name} Update";
                session.EnterUpdateLoop();
            } finally {
                Thread.CurrentThread.Name = oldThreadName;
            }
        })));
    }

    await Task.WhenAny([
        ..sessions.Select(x => x.SessionTask),
        Task.Delay(TimeSpan.FromMilliseconds(100))
    ]);
}

internal readonly record struct RunningSession(ISession Session, Task SessionTask);
