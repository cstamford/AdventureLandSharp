using AdventureLandSharp.SecretSauce;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.HttpApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Interfaces;
using InfluxDB.Client;
using StackExchange.Redis;

const string CREDENTIAL_user = "username";
const string CREDENTIAL_pass = "password";

GameData data = await Api.FetchGameDataAsync(Utils.ApiAddress);
World world = new(data, Utils.LoadSmapData());

ApiCredentials creds = new(CREDENTIAL_user, CREDENTIAL_pass);
ApiAuthState auth = await Api.LoginAsync(Utils.ApiAddress, creds);

if (!auth.Success) {
    throw new Exception("Failed to login.");
}

ServersAndCharactersResponse serversAndCharacters = await Api.ServersAndCharactersAsync(Utils.ApiAddress);
ISessionCoordinator coordinator = DependencyResolver.SessionCoordinator()(world, auth, serversAndCharacters);

if (coordinator is SessionCoordinator sessionCoordinator) {
    const string CREDENTIAL_influxUrl = "influxUrl";
    const string CREDENTIAL_influxToken = "influxToken";
    const string CREDENTIAL_influxBucket = Utils.TargetLocalServer ? "adventureland-debug" : "adventureland";
    const string CREDENTIAL_influxOrg = "influxOrg";

    try {  
        sessionCoordinator.Influx = new(new InfluxDBClientOptions(CREDENTIAL_influxUrl) {
            Token = CREDENTIAL_influxToken,
            Bucket = CREDENTIAL_influxBucket,
            Org = CREDENTIAL_influxOrg
        });
    } catch (Exception ex) {
        Log.Error($"Failed to connect to InfluxDB: {ex}");
    }

    const string CREDENTIAL_redisIp = "redisIp";
    const string CREDENTIAL_redisPassword = "redisPassword";
    const int redisDb = Utils.TargetLocalServer ? 2 : 1;

    try {
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(new ConfigurationOptions() {
            EndPoints = { CREDENTIAL_redisIp },
            Password = CREDENTIAL_redisPassword
        });
        sessionCoordinator.Redis = redis.GetDatabase(redisDb);
    }
    catch (Exception ex) {
        Log.Error($"Failed to connect to Redis: {ex}");
    }
}

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
