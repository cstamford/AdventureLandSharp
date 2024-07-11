using System.Diagnostics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Interfaces;

namespace AdventureLandSharp.SecretSauce.Character;

public abstract partial class CharacterBase : ICharacter {
    public override string ToString() => $"{GetType().Name} [{Me.Name}] {MyLoc}";

    public Socket Socket { get; }
    public abstract CharacterClass Class { get; }
    public LocalPlayer Entity => Socket.Player;
    public MapLocation EntityLocation => MyLoc;
    public CharacterConfig Cfg { get; }
    public World World { get; }

    public CharacterBase(World world, Socket socket, CharacterConfig config) {
        Log = new(socket.Player.Name);
        World = world;
        Socket = socket;
        Cfg = config;

        _actionBt = ActionBuild();
        _classBt = ClassBuild();
        _healBt = HealBuild();
        _movementBt = MovementBuild();
        _utilityBt = UtilityBuild();

        _lastPositionChangeTime = DateTimeOffset.Now;
    }

    public virtual bool Update() {
        SocketUpdate();
        TacticsUpdate();
        StrategyUpdate();

        if (Me.Dead) {
            Log.Warn(["DEAD"], $"{MovementStateDebugString}");
            Socket.Emit<Outbound.Respawn>(new());
            Thread.Sleep(TimeSpan.FromSeconds(1.0));
            return IsRunning;
        }

        if (Me.MapName == "jail" && MyLocLast.Map.Name != "jail") {
            Log.Warn(["JAIL"], $"{MovementStateDebugString}");
            return IsRunning;
        }

        if (DateTimeOffset.UtcNow.Subtract(_lastPositionChangeTime) >= TimeSpan.FromMinutes(5)) {
            Log.Warn(["STUCK"], $"{MovementStateDebugString}");
            ResetMovement();
            ForceJail();
            return IsRunning;
        }

        if (MyLoc.Map.Smap != null && (
            (EnemiesTargetingUs.Sum(x => x.Monster.AttackDamage)*2 >= Me.Health && Me.HealthPercent <= 35) || Me.HealthPercent <= 20)) {
            Log.Alert(["HEALTH"], $"Forcing jail due to low health.");
            ConsumePotion();
            ForceJail();
            return IsRunning;
        }

        if (Me.HealthPercent <= 5) {
            Log.Alert(["HEALTH"], $"Bailing due to critical health!");
            ConsumePotion();
            return false;
        }

        EventsUpdate();
        MovementUpdate();
        UtilityUpdate();

        if (!IsTeleporting || InCombat) {
            ActionUpdate();
            ClassUpdate();
            HealUpdate();
        }

        return IsRunning;
    }

    public static readonly TimeSpan NetworkThrottleIgnoreResponse = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan NetworkThrottleReadResponse = TimeSpan.FromMilliseconds(500);
    public const float NetworkSpamFastCooldownDivisor = NetworkSpamSlowCooldownDivisor; //1.0f / 24.75f;
    public const float NetworkSpamSlowCooldownDivisor = 1.0f / (6 + float.Epsilon);

    public void ForceJail() {
        Debug.Assert(MyLoc.Map.Smap != null);
        GameDataSmapCell firstInvalidCell = MyLoc.Map.Smap.Data.First(x => !x.Value.IsValid).Key;
        Socket.Emit<Outbound.Move>(new(Me.Position.X, Me.Position.Y, firstInvalidCell.X, firstInvalidCell.Y, Me.MapId));
        Thread.Sleep(TimeSpan.FromSeconds(0.25));
    }

    protected Logger Log { get; }
    protected LocalPlayer Me => Entity;
    protected bool IsRunning { get; set; } = true;
}
