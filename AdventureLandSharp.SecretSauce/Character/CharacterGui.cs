using System.Numerics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Example;
using AdventureLandSharp.Interfaces;
using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Tactics;

namespace AdventureLandSharp.SecretSauce;

#if WITH_GUI
using Raylib_cs;

public sealed class CharacterGui(World world, ICharacter character) : BasicCharacterGui(world, character) {
    protected override void Draw_2_Background() {
        base.Draw_2_Background();
        DrawPositioningPlan(
            Character.EntityLocation.Map,
            Character.PositioningPlan,
            Character.PositioningPlanWeights);
    }

    protected override void Draw_4_Foreground() {
        base.Draw_4_Foreground();

        DrawTacticsLabel(Character.ActiveTactics, (int)CamBounds.X + 4, (int)CamBounds.Y + 4 + 16);
        DrawStrategyLabel(Character.ActiveStrategy, (int)CamBounds.X + 4, (int)CamBounds.Y + 4 + 16 + 32);
        DrawEntityList(Character.Entity.Name, Character.Enemies, (int)CamBounds.X + 4, (int)CamBounds.Y + 4 + 16 + 128);

        DrawNetworkWindow("IncomingWindow", (int)(CamBounds.X + CamBounds.Width) - 196, (int)CamBounds.Y + 4, Character.Socket.IncomingMessages_10Secs.Values.Select(x => x.Data));
        DrawNetworkWindow("OutgoingWindow", (int)(CamBounds.X + CamBounds.Width) - 196, (int)(CamBounds.Y + CamBounds.Height/2), Character.Socket.OutgoingMessages_10Secs.Values.Select(x => x.Data));
    }

    private new CharacterBase Character => (CharacterBase)base.Character;

    private static void DrawPositioningPlan(Map map, IPositioningPlan plan, IReadOnlyList<GridWeight> planWeights) {
        if (planWeights.Count == 0) {
            return;
        }

        float minWeight = planWeights.Min(GetWeight);
        float maxWeight = planWeights.Max(GetWeight);

        foreach (GridWeight gridWeight in planWeights) {
            Vector2 pos = gridWeight.Grid.World(map);
            float weight = GetWeight(gridWeight);
            Color color = ColorFromWeight(weight, minWeight, maxWeight);
            Raylib.DrawRectangle((int)pos.X, (int)pos.Y, MapGridTerrain.CellSize, MapGridTerrain.CellSize, color);
        }

        static float GetWeight(GridWeight gridWeight) => gridWeight.Weights.Weight;

        static Color ColorFromWeight(float weight, float minWeight, float maxWeight) {
            float t = (weight - minWeight) / (maxWeight - minWeight);
            byte r = (byte)(255 * (1 - t));
            byte g = (byte)(255 * t);
            byte b = 0;
            byte a = 255;
            return new(r, g, b, a);
        }
    }

    private static void DrawTacticsLabel(ITactics tactics, int x, int y) {
        Raylib.DrawText($"{tactics.GetType().Name} {tactics.PositioningPlan.GetType().Name}\ntar: {tactics.AttackTarget?.ToString() ?? "(none)"}", x, y, 32, Color.White);
    }

    private static void DrawStrategyLabel(IStrategy strategy, int x, int y) {
        Raylib.DrawText($"{strategy.GetType().Name}\n{strategy}", x, y, 32, Color.White);
    }

    private static void DrawEntityList(string charName, IReadOnlyList<CachedMonster> monsters, int x, int y) {
        foreach (CachedMonster monster in monsters) {
            string line = $"{monster} HP:{monster.Monster.Health}/{monster.Monster.MaxHealth} DPS:{monster.Monster.DPS:f0}";
            Raylib.DrawText(line, x, y, 24, monster.Monster.Target == charName ? Color.Orange : Color.White);
            y += 32;
        }
    }

    private static void DrawNetworkWindow(string label, int x, int y, IEnumerable<string> events) {
        foreach ((string evt, int count) in events
            .GroupBy(x => x)
            .Select(y => (Event: y.Key, Count: y.Count()))
            .OrderByDescending(x => x.Count))
        {
            Raylib.DrawText($"{count} {evt}", x, y, 24, Color.White);
            y += 32;
        }
    }
}
#else
public sealed class CharacterGui(World world, ICharacter character) : BasicCharacterGui(world, character);
#endif