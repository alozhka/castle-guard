using CastleGuard.Level.Model;
using NUnit.Framework;

namespace CastleGuard.Tests.EditMode;

public class LevelRewardCalculatorTests
{
    [TestCase(29.999, false, false, 20)]
    [TestCase(29.999, true, false, 15)]
    [TestCase(29.999, false, true, 10)]
    [TestCase(29.999, true, true, 10)]
    [TestCase(30, false, false, 15)]
    [TestCase(30, true, false, 15)]
    [TestCase(30, false, true, 10)]
    [TestCase(30, true, true, 10)]
    [TestCase(30.001, false, false, 10)]
    [TestCase(30.001, true, false, 10)]
    [TestCase(30.001, false, true, 10)]
    [TestCase(30.001, true, true, 10)]
    public void Calculate_AppliesTimeAndAttemptRules(double seconds, bool usedUndo, bool usedHint, int total)
    {
        LevelReward reward = LevelRewardCalculator.Calculate(10, seconds, usedUndo, usedHint);

        Assert.That(reward.BaseReward, Is.EqualTo(10));
        Assert.That(reward.Multiplier, Is.EqualTo(total / 10m));
        Assert.That(reward.Total, Is.EqualTo(total));
    }

    [Test]
    public void Calculate_RoundsFractionalCoinsUp()
    {
        LevelReward reward = LevelRewardCalculator.Calculate(5, 30, false, false);

        Assert.That(reward.Total, Is.EqualTo(8));
    }
}
