using System;

namespace CastleGuard.Level.Model;

public static class LevelRewardCalculator
{
    public static LevelReward Calculate(int baseReward, double elapsedSeconds, bool usedUndo, bool usedHint)
    {
        ValidateBaseReward(baseReward);
        ValidateElapsedSeconds(elapsedSeconds);

        decimal multiplier = 1m;
        if (!usedHint && elapsedSeconds <= 30)
        {
            multiplier = !usedUndo && elapsedSeconds < 30 ? 2m : 1.5m;
        }

        return new LevelReward(baseReward, multiplier);
    }

    internal static void ValidateBaseReward(int baseReward)
    {
        if (baseReward is < 0 or > int.MaxValue / 2)
        {
            throw new ArgumentOutOfRangeException(nameof(baseReward));
        }
    }

    internal static void ValidateElapsedSeconds(double elapsedSeconds)
    {
        if (elapsedSeconds < 0 || double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds))
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        }
    }
}
