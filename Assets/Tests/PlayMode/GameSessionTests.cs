using CastleGuard.Level.Model;
using CastleGuard.App;
using CastleGuard.Economy;
using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleGuard.Tests.PlayMode;

public class GameSessionTests
{
    [UnityTest]
    public IEnumerator DirectLevelLaunchAndSceneChanges_KeepGameAndProgress()
    {
        yield return SceneManager.LoadSceneAsync("Level");
        yield return null;

        GameModel game = GameSession.Model;
        CoinWallet wallet = game.Progress.Wallet;
        int balance = wallet.Balance;
        Assert.That(game.Level, Is.Not.Null);
        Assert.That(game.Level.HintPrice, Is.EqualTo(10));

        yield return SceneManager.LoadSceneAsync("MainMenu");
        yield return null;
        Assert.That(GameSession.Model, Is.SameAs(game));
        Assert.That(game.Progress.Wallet, Is.SameAs(wallet));
        Assert.That(game.Level, Is.Null);
        Assert.That(game.CurrentLevel, Is.Null);

        yield return SceneManager.LoadSceneAsync("Level");
        yield return null;
        Assert.That(GameSession.Model, Is.SameAs(game));
        Assert.That(game.Progress.Wallet, Is.SameAs(wallet));
        Assert.That(game.Progress.Wallet.Balance, Is.EqualTo(balance));
        Assert.That(game.Level, Is.Not.Null);
    }
}
