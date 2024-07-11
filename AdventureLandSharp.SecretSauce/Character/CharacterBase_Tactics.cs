using System.Numerics;
using AdventureLandSharp.SecretSauce.Tactics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.SecretSauce.Character;

public abstract partial class CharacterBase {
    public ITactics ActiveTactics => _tactics.First(x => x.Active);
    public IReadOnlyList<GridWeight> PositioningPlanWeights => _positioningPlanWeights;

    public virtual CachedMonster? AttackTarget => ActiveTactics.AttackTarget;
    public virtual IPositioningPlan PositioningPlan => ActiveTactics.PositioningPlan;
    public virtual bool InCombat => ActiveTactics is not Base_TravelTactics;

    protected List<ITactics> AvailableTactics => _tactics;
    protected DateTimeOffset? AttackTargetLostAt { get; private set; }
    protected TimeSpan TimeSinceAttackTargetLost => DateTimeOffset.UtcNow.Subtract(AttackTargetLostAt ?? DateTimeOffset.UtcNow);

    protected virtual void OnTactics() {
        _tactics.Add(new HighValue_PinkGoblinTactics(this));
        _tactics.Add(new HighValue_PhoenixTactics(this));
        _tactics.Add(new Base_CombatTactics(this));
        _tactics.Add(new Base_TravelTactics(this));
    }

    protected virtual void TacticsUpdate() {
        foreach (ITactics tactic in _tactics.Where(x => x.Active)) {
            tactic.Update();
        }

        if (AttackTarget.HasValue) {
            AttackTargetLostAt = null;
        } else {
            AttackTargetLostAt ??= DateTimeOffset.UtcNow;
        }
    }

    private List<ITactics> _tactics = [];
    private List<GridWeight> _positioningPlanWeights = [];
}

public interface IPositioningWeights {
    float Weight { get; }
}

public record struct PositioningWeights(float Weight) : IPositioningWeights;

public record struct GridWeight(MapGridCell Grid, IPositioningWeights Weights) {
    public readonly float Weight => Weights.Weight;
    public readonly T GetWeights<T>() where T : struct, IPositioningWeights => (T)Weights;
}

public interface IPositioningPlan {
    public GridWeight GetPosition(IReadOnlyList<GridWeight> weights);
    public void StoreWeights(List<GridWeight> weights);
}

public abstract class PositioningPlan(CharacterBase me) : IPositioningPlan {
    public abstract GridWeight GetPosition(IReadOnlyList<GridWeight> weights);
    public abstract void StoreWeights(List<GridWeight> weights);

    protected MapLocation MyLoc => me.EntityLocation;
    protected MapGridCell MyLocGrid => MyLoc.Grid();
    protected LocalPlayer Me => me.Entity;
    protected CharacterBase MyChar => me;
    protected IReadOnlyList<CachedEntity> Entities => me.Entities;
    protected IReadOnlyList<CachedPlayer> Players => me.Players;
    protected IReadOnlyList<CachedPlayer> PartyPlayers => me.PartyPlayers;
    protected IReadOnlyList<CachedMonster> Enemies => me.Enemies;
    protected IReadOnlyList<CachedMonster> EnemiesInRange => me.EnemiesInRange;
    protected IReadOnlyList<CachedMonster> EnemiesTargetingUs => me.EnemiesTargetingUs;
    protected IReadOnlyList<CachedMonster> EnemiesNotTargetingUs => me.EnemiesNotTargetingUs;
    protected IReadOnlyList<CachedMonster> BlacklistedEnemies => me.BlacklistedEnemies;
    protected IReadOnlyList<CachedNpc> Npcs => me.Npcs;

    protected CachedMonster BestAttackTarget => me.AttackTarget?.Priority >= Enemies[0].Priority ? me.AttackTarget.Value : Enemies[0];

    protected static int Width => (int)Math.Ceiling(GameConstants.VisionWidth/2.0f / MapGridTerrain.CellSize);
    protected static int Height => (int)Math.Ceiling(GameConstants.VisionHeight/2.0f / MapGridTerrain.CellSize);

    protected int AttackRangeX => (int)Math.Ceiling((Me.AttackRange + Me.Size.X/2) / MapGridTerrain.CellSize);
    protected int AttackRangeY => (int)Math.Ceiling((Me.AttackRange + Me.Size.Y/2) / MapGridTerrain.CellSize);

    protected IEnumerable<CachedMonster> IntersectsWith(Vector2 from, Vector2 to) => Enemies
        .Where(x => Collision.LineVsCircle(from, to, x.Position) < x.Monster.AttackRange + MathF.Max(x.Monster.Size.X, x.Monster.Size.Y));

    protected float DistanceFromPos(Entity e, Vector2 pos) => Collision.Dist(e.Position, pos, e.Size, Me.Size) + MapGridTerrain.Epsilon;
    protected float DistanceFromPos(CachedMonster enemy, Vector2 pos) => DistanceFromPos(enemy.Monster, pos);
    protected bool InAttackRange(CachedMonster enemy, Vector2 pos) => DistanceFromPos(enemy, pos) < Me.AttackRange - Me.ExtraRange/2;
    protected bool InEnemyAttackRange(CachedMonster enemy, Vector2 pos) => DistanceFromPos(enemy, pos) < enemy.Monster.AttackRange + enemy.Monster.Speed*0.2f;
    protected bool IsWalkable(Vector2 pos) {
        MapGridCellData data = pos.Data(MyLoc.Map);
        return data.IsWalkable && data.PHashScore == 0 && data.RpHashScore == 0;
     }
    protected bool IsPartyLeaderInRange(Vector2 pos) {
        if (!MyChar.Cfg.PartyLeaderFollowDist.HasValue) {
            return true;
        }

        CachedPlayer? leader = PartyPlayers.FirstOrNull(x => x.Player.Name == MyChar.Cfg.PartyLeader);
        return leader == null || DistanceFromPos(leader.Value.Player, pos) <= MyChar.Cfg.PartyLeaderFollowDist;
    }

    protected bool IsOccupiedByPlayer(Vector2 pos) => 
        Players.Any(x => x.Position.Equivalent(pos, 16));

    protected bool ValidatePosition(CachedMonster enemy, Vector2 pos) =>
        IsWalkable(pos) &&
        InAttackRange(enemy, pos) &&
        !IsOccupiedByPlayer(pos) &&
        (IsPartyLeaderInRange(pos) || enemy.PriorityType >= TargetPriorityType.Priority);
}

public sealed class NullPositioningPlan(CharacterBase me) : PositioningPlan(me) {
    public override GridWeight GetPosition(IReadOnlyList<GridWeight> weights) => weights[0];
    public override void StoreWeights(List<GridWeight> weights) => weights.Add(new(MyLocGrid, new PositioningWeights()));
}
