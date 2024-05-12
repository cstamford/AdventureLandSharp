using System.Numerics;

namespace AdventureLandSharp.Core;

public readonly record struct GameDataSmapCell(int X, int Y) {
    public string Key => $"{X}|{Y}";
    public override int GetHashCode() => X | (Y << 16);

    public GameDataSmapCell(string cell) : this(0, 0) {
        string[] parsed = cell.Split('|');
        X = int.Parse(parsed[0]);
        Y = int.Parse(parsed[1]);
    }

    public static GameDataSmapCell FromRpHash(Vector2 pos) => new(
        X: SmapRound(pos.X),
        Y: SmapRound(pos.Y)
    );

    public static GameDataSmapCell FromPhash(Vector2 pos) => new(
        X: (int)(pos.X - (pos.X % GameDataSmap.StepSize)),
        Y: (int)(pos.Y - (pos.Y % GameDataSmap.StepSize))
    );

    private static int SmapRound(float x) {
        float x1 = x - (x % GameDataSmap.StepSize);
        float x2 = x1 + GameDataSmap.StepSize;
        if (x < 0) {
            x2 = x1 - GameDataSmap.StepSize;
        }
        return MathF.Abs(x - x1) < MathF.Abs(x - x2) ? (int)x1 : (int)x2;
    }
}

public readonly record struct GameDataSmapCellData(ushort Value) {
    public bool IsValid => Value < JailValue;

    public const ushort MinValue = 0;
    public const ushort JailValue = 2;
    public const ushort MaxValue = 8;

    public static GameDataSmapCellData Valid => new(MinValue);
    public static GameDataSmapCellData Invalid => new(MaxValue);
}

public class GameDataSmap(Dictionary<string, ushort> smap) {
    public const int StepSize = 10;
    public const int EdgeSize = 60;
    public IReadOnlyDictionary<GameDataSmapCell, GameDataSmapCellData> Data => _smap;

    public GameDataSmapCellData RpHash(Vector2 pos) =>
         _smap.TryGetValue(GameDataSmapCell.FromRpHash(pos), out GameDataSmapCellData data) ? data : GameDataSmapCellData.Invalid;
    public GameDataSmapCellData PHash(Vector2 pos) => 
        _smap.TryGetValue(GameDataSmapCell.FromPhash(pos), out GameDataSmapCellData data) ? data : GameDataSmapCellData.Invalid;

    private readonly Dictionary<GameDataSmapCell, GameDataSmapCellData> _smap = smap.ToDictionary(
        y => new GameDataSmapCell(y.Key),
        y => new GameDataSmapCellData(y.Value)
    );
}