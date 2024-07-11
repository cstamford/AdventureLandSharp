using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Classes;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.HttpApi;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Interfaces;
using InfluxDB.Client;
using StackExchange.Redis;

namespace AdventureLandSharp.SecretSauce;

public class SessionCoordinator(
    World world,
    ApiAuthState authState,
    ServersAndCharactersResponse serverState) : ISessionCoordinator
{
    [SessionCoordinatorFactory]
    public static ISessionCoordinator Create(World world, ApiAuthState apiAuthState, ServersAndCharactersResponse api)
       => new SessionCoordinator(world, apiAuthState, api);

    public InfluxDBClient? Influx { get; set; }
    public IDatabase? Redis { get; set; }
    public IEnumerable<SessionPlan> Plans => FetchConfigs()
        .Select(x => (Character: serverState.Characters.First(c => c.Name == x.Key), Config: x.Value))
        .Select(x => MakeSessionPlan(x.Character, x.Config, withGui: Enum.Parse<CharacterClass>(x.Character.Type, ignoreCase: true) == CharacterClass.Priest));

    private WriteApi? _influxWriteApi = null;
    private readonly SessionEventBus _eventBus = new();
    private readonly ConcurrentDictionary<string, SessionEventBusHandle> _eventBusHandles = [];
    private Dictionary<string, CharacterConfig> _characterConfigs = [];
    private readonly SemaphoreSlim _characterConfigsLock = new(1);
    private DateTimeOffset _characterConfigsNext = DateTimeOffset.UtcNow;
    private DateTimeOffset _HACK_lastPriorityMobSeen = DateTimeOffset.MinValue;
    private DateTimeOffset _HACK_lastCharacterLayoutChanged = DateTimeOffset.MinValue;
    private Dictionary<string, CharacterConfig> _HACK_lastCharacterConfigs = [];

    private SessionPlan MakeSessionPlan(ApiCharacter character, CharacterConfig config, bool withGui) => new(
        Connection: MakeConnectionSettings(character),
        SessionFactory: (w, s, c, g) => MakeSession(w, s, c, g, config.PartyLeader),
        CharacterFactory: (w, s, cls) => MakeCharacter(w, s, cls, config),
        GuiFactory: withGui ? ((w, c) => MakeCharacterGui(w, c)) : null
    );

    private ConnectionSettings MakeConnectionSettings(ApiCharacter character) => new(
        UserId: authState.UserId,
        AuthToken: authState.AuthToken,
        Server: serverState.Servers.First(x => x is { Region: Utils.TargetLocalServer ? "US" : "EU",  Name: Utils.TargetLocalServer ? "III" : "I" }),
        Character: character
    );

    private Session MakeSession(World world, ConnectionSettings settings, CharacterFactory characterFactory, GuiFactory? guiFactory, string partyLeader) {
        _influxWriteApi ??= Influx?.GetWriteApi();
        Session session = new(world, settings, characterFactory, guiFactory, _influxWriteApi, Redis);

        session.OnInit += (s, c) => {
            s.OnPartyInvite += (evt) => {
                if (evt.Name == partyLeader) {
                    Log.Alert([c.Entity.Name], $"Accepting party invite from {evt.Name}.");
                    s.Emit<Outbound.PartyInviteAccept>(new(evt.Name));
                }
            };

            s.OnPartyRequest += (evt) => {
                if (settings.Character.Name == partyLeader) {
                    Log.Alert([c.Entity.Name], $"Accepting party request from {evt.Name}.");
                    s.Emit<Outbound.PartyInviteAcceptRequest>(new(evt.Name));
                }
            };
        };

        DateTimeOffset lastPartyInvite = DateTimeOffset.MinValue;

        session.OnTick += (s, c) => {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (settings.Character.Name != partyLeader &&
                !s.Party.Any(x => x == partyLeader) &&
                now - lastPartyInvite > TimeSpan.FromSeconds(5))
            {
                Log.Alert([c.Entity.Name], $"Sending party invite to {partyLeader}.");
                s.Emit<Outbound.PartyInviteRequest>(new(partyLeader));
                lastPartyInvite = now;
            }

            //HACK_UpdatePriorityMob(c);
        };

        session.OnFree += (s, c) => {
            if (c is CharacterBase character) {
                _eventBus.UnregisterAll(character);
                _eventBusHandles.Remove(s.Player.Name, out _);
            }
        };

        return session;
    }

    private CharacterBase MakeCharacter(World world, Socket socket, CharacterClass cls, CharacterConfig config) {
        if (_eventBusHandles.TryGetValue(socket.Player.Name, out SessionEventBusHandle? handle)) {
            handle.Dispose();
        }

        CharacterBase character = cls switch {
            CharacterClass.Mage => new Mage(world, socket, config),
            CharacterClass.Merchant => new Merchant(world, socket, config),
            CharacterClass.Priest => new Priest(world, socket, config),
            CharacterClass.Ranger => new Ranger(world, socket, config),
            CharacterClass.Rogue => new Rogue(world, socket, config),
            CharacterClass.Warrior => new Warrior(world, socket, config),
            _ => throw new()
        };

        handle = new SessionEventBusHandle(_eventBus, character);
        _eventBusHandles[socket.Player.Name] = handle;
        character.EventBusHandle = handle;

        return character;
    }

    private static CharacterGui MakeCharacterGui(World world, ICharacter character) => new(world, character);

    private Dictionary<string, CharacterConfig> FetchConfigs() {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (now >= _characterConfigsNext) {
            Debug.Assert(Redis != null);
            Task task = Redis.StringGetAsync("v2_config").ContinueWith(task => {
                JsonSerializerOptions opts = JsonOpts.Default.AddMapLocationConverter(world);
                string[] characters = JsonSerializer.Deserialize<string[]>(task.Result!, opts)!;
                Dictionary<string, CharacterConfig> configs = characters
                    .Select(x => (Name: x, Config: Redis.StringGet($"v2_config_{x.ToLower()}")))
                    .Select(x => (x.Name, JsonSerializer.Deserialize<CharacterConfig>(x.Config!, opts)))
                    .ToDictionary();

                _characterConfigsLock.Wait();
                _characterConfigs = configs;
                _characterConfigsLock.Release();
            });

            bool firstTime = _eventBusHandles.IsEmpty;
            if (firstTime) {
                task.Wait();
            }

            _characterConfigsNext = now.AddSeconds(5);
        }

        Dictionary<string, CharacterConfig> configs;

        _characterConfigsLock.Wait();
        configs = _characterConfigs;
        _characterConfigsLock.Release();

        // TEMP HACK: During the priority mob downtime, we swap the priest to the rogue to help
        //            with Atlus' one-eye farm..
        /*
        bool needPriest = now.Subtract(_HACK_lastPriorityMobSeen) < TimeSpan.FromSeconds(1);
        IEnumerable<string> currentChars = _eventBusHandles.Select(x => x.Key);
        IEnumerable<string> desiredChars = configs.Keys.Where(x => x != (needPriest ? "Sneakmato" : "Healmato"));

        if (now.Subtract(_HACK_lastCharacterLayoutChanged) >= TimeSpan.FromSeconds(30) && 
            !currentChars.Order().SequenceEqual(desiredChars)) {
            Log.Alert(["HACK"], $"Swapping characters from {currentChars} to {desiredChars}.");
            _HACK_lastCharacterLayoutChanged = now;
            _HACK_lastCharacterConfigs = configs.Where(x => desiredChars.Contains(x.Key)).ToDictionary();
        }

        return _HACK_lastCharacterConfigs;
        */
        return configs;
    }

    private void HACK_UpdatePriorityMob(ICharacter c) {
        Dictionary<string, CharacterConfig> configs;

        _characterConfigsLock.Wait();
        configs = _characterConfigs;
        _characterConfigsLock.Release();

        // TEMP HACK
        if (c is CharacterBase character && configs.TryGetValue("Healmato", out CharacterConfig priestConfig)) {
            bool hasPriorityMob = character.Entities.Any(x => 
                x.Entity is Monster m &&
                m.HealthPercent >= 50 &&
                priestConfig.GetTargetPriorityType(m.Type) == TargetPriorityType.Priority
            );

            if (hasPriorityMob) {
                _characterConfigsLock.Wait();
                _HACK_lastPriorityMobSeen = DateTimeOffset.UtcNow;
                _characterConfigsLock.Release();
            }
        }
    }
}

public record struct SessionCoordinatorConfig(
    string[] CharactersActive
);
