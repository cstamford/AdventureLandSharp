using System.Numerics;
using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.SecretSauce.Tactics;

public class Base_CombatTactics(CharacterBase me) : TacticsBase(me) {
    public override bool Active => 
        EnemiesTargetingUs.Any(x => x.Distance <= x.Monster.AttackRange + x.Monster.Speed*2) || 
        (!MyChar.Withdrawing && Enemies.Any(x => x.PriorityType >= TargetPriorityType.Normal));

    public override CachedMonster? AttackTarget => 
        EnemiesTargetingUs.FirstOrNull(x => x.Distance <= Me.AttackRange) ?? 
        EnemiesInRange.FirstOrNull(x => x.PriorityType >= TargetPriorityType.Normal) ??
        Enemies.FirstOrNull(x => x.PriorityType >= TargetPriorityType.Opportunistic);

    public override IPositioningPlan PositioningPlan => 
        Me.AttackRange < 96 ? new MeleePositioningPlan(MyChar) : new RangedPositioningPlan(MyChar);
}

// TODO: The below accounts for most of frame time: we have enough CPU cycles to hit frame rate target, so I haven't
//       optimized this. We should consider limiting how often this can occur, or partially updating the weights.
//       Partial update: do 1/8th each frame and cycle through the grid rather than all at once.

public record struct MeleePositioningWeights(
    int CitizensInRange,
    float Weight
) : IPositioningWeights;

public sealed class MeleePositioningPlan(CharacterBase me) : PositioningPlan(me) {
    public override GridWeight GetPosition(IReadOnlyList<GridWeight> weights) => weights
        .OrderByDescending(x => x.GetWeights<MeleePositioningWeights>().CitizensInRange)
        .ThenByDescending(x => x.Weight)
        .First();

    public override void StoreWeights(List<GridWeight> weights) {
        IEnumerable<CachedMonster> validEnemies = Enemies.Where(x => x.Priority >= Enemies[0].Priority || x.PriorityType == TargetPriorityType.Opportunistic);
        IEnumerable<CachedMonster> trashEnemies = Enemies.Where(x => x.PriorityType <= TargetPriorityType.Ignore);
        CachedMonster tar = BestAttackTarget;

        for (int x = -AttackRangeX; x <= AttackRangeX; ++x) {
            for (int y = -AttackRangeY; y <= AttackRangeY; ++y) {
                MapGridCell cell = new(MyLocGrid, x, y);
                MapGridCellData cellData = cell.Data(MyLoc.Map);
                Vector2 pos = cell.World(MyLoc.Map);

                if (!ValidatePosition(tar, pos)) {
                    continue;
                }

                int citizensInRange = Npcs.Count(x => x.InAuraRange(pos));
                int enemiesInRange = validEnemies.Count(x => InAttackRange(x, pos));
                int enemiesWhoCanHitUs = validEnemies.Count(x => InEnemyAttackRange(x, pos));

                float trashWeight = trashEnemies.Sum(x => DistanceFromPos(x, pos));
                float coneDot = (1 + Vector2.Dot(Vector2.Normalize(pos - tar.Position), Vector2.Normalize(MyLoc.Position - tar.Position))) / 2;
                float kiteWeight = EnemiesTargetingUs.Sum(x => DistanceFromPos(x, pos)) * coneDot;
                float nearNextWeight = validEnemies.Sum(x => DistanceFromPos(x, pos));

                float weight = kiteWeight + trashWeight - nearNextWeight;
                weight -= MathF.Pow(weight, cellData.CornerScore);
                weight *= enemiesInRange + 1;
                weight /= enemiesWhoCanHitUs * (Me.HealthPercent <= 50 ? 2.0f : 0.5f) + 1;

                weights.Add(new(pos.Grid(MyLoc.Map), new MeleePositioningWeights(citizensInRange, weight)));
            }
        }

        if (weights.Count == 0) {
            weights.Add(new(tar.Position.Grid(MyLoc.Map), new MeleePositioningWeights()));
        }
    }
}

public record struct RangedPositioningWeights(
    int CitizensInRange,
    int EnemiesInRange,
    float Weight
) : IPositioningWeights;

public sealed class RangedPositioningPlan(CharacterBase me) : PositioningPlan(me) {
    public override GridWeight GetPosition(IReadOnlyList<GridWeight> weights) => weights
        .OrderByDescending(x => x.GetWeights<RangedPositioningWeights>().CitizensInRange)
        .ThenByDescending(x => x.GetWeights<RangedPositioningWeights>().EnemiesInRange)
        .ThenBy(x => x.Weight)
        .First();

    public override void StoreWeights(List<GridWeight> weights) {
        IEnumerable<CachedMonster> validEnemies = Enemies.Where(x => x.Priority >= Enemies[0].Priority);
        CachedMonster tar = BestAttackTarget;

        for (int x = -AttackRangeX; x <= AttackRangeX; ++x) {
            for (int y = -AttackRangeY; y <= AttackRangeY; ++y) {
                MapGridCell cell = new(MyLocGrid, x, y);
                MapGridCellData cellData = cell.Data(MyLoc.Map);
                Vector2 pos = cell.World(MyLoc.Map);

                if (!ValidatePosition(tar, pos)) {
                    continue;
                }

                int citizensInRange = Npcs.Count(x => x.InAuraRange(pos));
                int enemiesInRange = validEnemies.Count(x => InAttackRange(x, pos));
                float enemiesWeight = validEnemies.Sum(x => DistanceFromPos(x, pos));

                weights.Add(new(pos.Grid(MyLoc.Map), new RangedPositioningWeights(citizensInRange, enemiesInRange, enemiesWeight)));
            }
        }

        if (weights.Count == 0) {
            weights.Add(new(tar.Position.Grid(MyLoc.Map), new RangedPositioningWeights()));
        }
    }
}
