using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Example;
using AdventureLandSharp.Interfaces;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using StackExchange.Redis;

namespace AdventureLandSharp.SecretSauce;

public class Session : BasicSession {
    public Session(
        World world,
        ConnectionSettings settings,
        CharacterFactory characterFactory,
        GuiFactory? guiFactory,
        WriteApi? influxWriteApi,
        IDatabase? redis) : base(world, settings, characterFactory, guiFactory)
    {
        Debug.Assert(influxWriteApi != null);
        SetupTelemetry(influxWriteApi);

        Debug.Assert(redis != null);
        SetupCharacterMirror(redis);
    }

    private DateTimeOffset _lastTick = DateTimeOffset.UtcNow;
    private readonly MovingAverage<double> _tickRate = new(TimeSpan.FromSeconds(1));

    private DateTimeOffset _lastTickWrite = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastStateWrite = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastBankWrite = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastNetWrite = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastPingWrite = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastTracker = DateTimeOffset.UtcNow;

    private JsonElement _playerEntityData = default;
    private JsonElement _playerItemsData = default;

    private readonly HttpClient _client = new();
    private readonly HashSet<string> _killedByHits = [];

    private void SetupTelemetry(WriteApi influxWriteApi) {
        OnInit += (socket, character) => {
            socket.OnDeath += evt => {
                if (!_killedByHits.Contains(evt.Id)) {
                    return;
                }

                if (socket.TryGetEntity(evt.Id, out Entity e)) {
                    PointData kill = PointData.Measurement("kill")
                        .Tag("character", character.Entity.Name)
                        .Tag("map", character.Entity.MapName)
                        .Tag("npc_name", e.Name)
                        .Tag("npc_type", e.Type)
                        .Field("x", e.Position.X)
                        .Field("y", e.Position.Y)
                        .Field("luck_chance", evt.LuckChance)
                        .Timestamp(DateTimeOffset.UtcNow, WritePrecision.Ns);

                    Task.Run(() => influxWriteApi.WritePoint(kill));
                }

                _killedByHits.Remove(evt.Id);
            };

            socket.OnHit += evt => {
                if (evt.OwnerId != character.Entity.Id) {
                    return;
                }

                PointData hit = PointData.Measurement("hit")
                    .Tag("character", character.Entity.Name)
                    .Tag("map", character.Entity.MapName)
                    .Tag("source", evt.Source)
                    .Field("damage", evt.Damage)
                    .Field("crit_multi", evt.CritMultiplier)
                    .Field("lifesteal_hp", evt.LifestealHp)
                    .Field("kill", evt.Kill)
                    .Field("miss", evt.Miss)
                    .Timestamp(DateTimeOffset.UtcNow, WritePrecision.Ns);

                Task.Run(() => influxWriteApi.WritePoint(hit));

                if (evt.Kill) {
                    _killedByHits.Add(evt.TargetId);
                }
            };
        };

        OnTick += (socket, character) => {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            _tickRate.Add(now.Subtract(_lastTick).TotalMilliseconds);
            _lastTick = now;

            if (now.Subtract(_lastTickWrite) >= TimeSpan.FromSeconds(1)) {
                PointData tickRate = PointData.Measurement("tick_rate")
                    .Tag("character", character.Entity.Name)
                    .Field("min", _tickRate.Min)
                    .Field("avg", _tickRate.Sum / _tickRate.Samples)
                    .Field("max", _tickRate.Max)
                    .Timestamp(now, WritePrecision.S);

                Task.Run(() => influxWriteApi.WritePoint(tickRate));
                _lastTickWrite = now;
            }

            if (now.Subtract(_lastStateWrite) >= TimeSpan.FromSeconds(5)) {
                PointData state = PointData.Measurement("state")
                    .Tag("character", character.Entity.Name)
                    .Tag("map", character.Entity.MapName)
                    .Field("x", character.Entity.Position.X)
                    .Field("y", character.Entity.Position.Y)
                    .Field("health_percent", character.Entity.HealthPercent)
                    .Field("mana_percent", character.Entity.ManaPercent)
                    .Field("xp", character.Entity.Stats.Xp + _world.Data.Levels[character.Entity.Level])
                    .Field("level", character.Entity.Level)
                    .Field("gold", character.Entity.Inventory.Gold)
                    .Field("inv_slots_free", character.Entity.Inventory.SlotsFree)
                    .Field("inv_slots_used", character.Entity.Inventory.SlotsUsed)
                    .Field("cc", character.Entity.CodeCallCost)
                    .Timestamp(now, WritePrecision.S);

                Task.Run(() => influxWriteApi.WritePoint(state));
                _lastStateWrite = now;
            }

            if (now.Subtract(_lastBankWrite) >= TimeSpan.FromMinutes(15) && character.Entity.Bank.HasValue) {
                PointData stateBank = PointData.Measurement("state_bank")
                    .Field("bank_slots_free", character.Entity.Bank.Value.SlotsFree)
                    .Field("bank_slots_used", character.Entity.Bank.Value.SlotsUsed)
                    .Timestamp(now, WritePrecision.S);

                Task.Run(() => influxWriteApi.WritePoint(stateBank));
                SendBankToEarthiverse(character.Entity.OwnerId, character.Entity.Bank.Value);
                SendReportToDiocles(character.Entity.Name, character.Entity.Bank.Value);
                _lastBankWrite = now;
            }

            if (now.Subtract(_lastNetWrite) >= TimeSpan.FromSeconds(5)) {
                IEnumerable<(string evt, int count)> incomingEvts = character.Socket.IncomingMessages_10Secs.Values
                    .GroupBy(x => x.Data)
                    .Select(y => (Event: y.Key, Count: y.Count()))
                    .OrderByDescending(x => x.Count);

                IEnumerable<(string evt, int count)> outgoingEvts = character.Socket.OutgoingMessages_10Secs.Values
                    .GroupBy(x => x.Data)
                    .Select(y => (Event: y.Key, Count: y.Count()))
                    .OrderByDescending(x => x.Count);

                PointData[] incomingPoints = [..incomingEvts.Select(x => PointData.Measurement("net")
                    .Tag("character", character.Entity.Name)
                    .Tag("direction", "incoming")
                    .Tag("event", x.evt)
                    .Field("count", x.count)
                    .Timestamp(now, WritePrecision.S))];

                PointData[] outgoingPoints = [..outgoingEvts.Select(x => PointData.Measurement("net")
                    .Tag("character", character.Entity.Name)
                    .Tag("direction", "outgoing")
                    .Tag("event", x.evt)
                    .Field("count", x.count)
                    .Timestamp(now, WritePrecision.S))];

                Task.Run(() => {
                    influxWriteApi.WritePoints(incomingPoints);
                    influxWriteApi.WritePoints(outgoingPoints);
                });

                _lastNetWrite = now;
            }

            if (now.Subtract(_lastPingWrite) >= TimeSpan.FromSeconds(1)) {
                TimeSpan latency = socket.Latency;
                PointData ping = PointData.Measurement("ping")
                    .Tag("character", character.Entity.Name)
                    .Field("latency", latency.TotalMilliseconds)
                    .Timestamp(now, WritePrecision.Ms);

                Task.Run(() => influxWriteApi.WritePoint(ping));
                _lastPingWrite = now;
            }
        };
    }

    private void SetupCharacterMirror(IDatabase redis) {
        OnInit += (socket, character) => {
            socket.OnPlayer += evt => {
                _playerEntityData = evt;
                _playerItemsData = evt.TryGetProperty("items", out JsonElement items) ? items : _playerItemsData;
            };

            socket.OnTracker += evt => {
                string key = $"v2_mirror_{character.Entity.Id.ToLower()}";

                if (_playerEntityData.ValueKind == JsonValueKind.Undefined ||
                    _playerItemsData.ValueKind == JsonValueKind.Undefined)
                {
                    Log.Warn($"Can't update {key}. _playerEntityData: {_playerEntityData}, _playerItemsData: {_playerItemsData}.");
                    return;
                }

                Dictionary<string, JsonElement> playerData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(_playerEntityData.GetRawText())!;
                playerData["items"] = _playerItemsData;
                playerData["tracker"] = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new {
                    monsters = evt.Monsters,
                    monsters_diff = evt.MonstersDiff,
                    max = evt.Max,
                }));

                redis.StringSetAsync(key, JsonSerializer.Serialize(playerData));
                Log.Alert($"Updated {key}.");
            };
        };

        OnTick += (socket, character) => {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (now.Subtract(_lastTracker) >= TimeSpan.FromMinutes(1)) {
                // TODO: After equip_batch and CC issues solved
                //socket.Emit<Outbound.Tracker>(new());
                _lastTracker = now;
            }
        };
    }

    private void SendBankToEarthiverse(string owner, PlayerBank bank) {
        string serializedBank = JsonSerializer.Serialize(bank);
        const string CREDENTIAL_earthiverseKey = "earthiverseKey";
        HttpRequestMessage request = new(HttpMethod.Put, $"https://aldata.earthiverse.ca/bank/{owner}/{CREDENTIAL_earthiverseKey}") {
            Content = new StringContent(serializedBank, Encoding.UTF8, "application/json")
        };
        SendHttpRequestWithLogging(request);
    }

    public void SendReportToDiocles(string name, PlayerBank bank) {
        const string CREDENTIAL_telegramToken = "telegramToken";
        const string CREDENTIAL_telegramChatId = "telegramChatId";

        int dexEarrings0 = bank.ValidTabs.Sum(x => x.Tab.Count(y => y.HasValue && y.Value.Name == "dexearring" && y.Value.Level == 0));
        int dexEarrings1 = bank.ValidTabs.Sum(x => x.Tab.Count(y => y.HasValue && y.Value.Name == "dexearring" && y.Value.Level == 1));
        int dexEarrings2 = bank.ValidTabs.Sum(x => x.Tab.Count(y => y.HasValue && y.Value.Name == "dexearring" && y.Value.Level == 2));

        int strEarrings0 = bank.ValidTabs.Sum(x => x.Tab.Count(y => y.HasValue && y.Value.Name == "strearring" && y.Value.Level == 0));
        int strEarrings1 = bank.ValidTabs.Sum(x => x.Tab.Count(y => y.HasValue && y.Value.Name == "strearring" && y.Value.Level == 1));
        int strEarrings2 = bank.ValidTabs.Sum(x => x.Tab.Count(y => y.HasValue && y.Value.Name == "strearring" && y.Value.Level == 2));

        int intEarrings0 = bank.ValidTabs.Sum(x => x.Tab.Count(y => y.HasValue && y.Value.Name == "intearring" && y.Value.Level == 0));
        int intEarrings1 = bank.ValidTabs.Sum(x => x.Tab.Count(y => y.HasValue && y.Value.Name == "intearring" && y.Value.Level == 1));
        int intEarrings2 = bank.ValidTabs.Sum(x => x.Tab.Count(y => y.HasValue && y.Value.Name == "intearring" && y.Value.Level == 2));

        string message = 
            $"Thank you for subscribing to {name}'s jewellery report service.\n" +
            $"Dex earrings: l0={dexEarrings0} l1={dexEarrings1} l2={dexEarrings2}\n" +
            $"Str earrings: l0={strEarrings0} l1={strEarrings1} l2={strEarrings2}\n" +
            $"Int earrings: l0={intEarrings0} l1={intEarrings1} l2={intEarrings2}";

        HttpRequestMessage request = new(HttpMethod.Post, $"https://api.telegram.org/bot{CREDENTIAL_telegramToken}/sendMessage") {
            Content = JsonContent.Create(new { chat_id = CREDENTIAL_telegramChatId, text = message })
        };

        SendHttpRequestWithLogging(request);
    }

    protected override void DisposeInternal() {
        _client.Dispose();
        base.DisposeInternal();
    }

    private void SendHttpRequestWithLogging(HttpRequestMessage message) {
        if (Utils.TargetLocalServer) {
            _log.Info($"Would send request: {message}");
            message.Dispose();
            return;
        }

        _client.SendAsync(message).ContinueWith(x => {
            try {
                if (x.Result.IsSuccessStatusCode) {
                    Log.Alert($"Sent message to {message.RequestUri}.");
                } else {
                    Log.Warn($"Failed to send message to {message.RequestUri}. Status code: {x.Result.StatusCode}");
                }
            } catch (Exception e) {
                Log.Error($"Failed to send message to {message.RequestUri}. Exception: {e}");
            } finally {
                message.Dispose();
            }
        });
    }
}
