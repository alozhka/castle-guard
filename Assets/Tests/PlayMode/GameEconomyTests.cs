using CastleGuard.App;
using CastleGuard.Economy;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleGuard.Tests.PlayMode;

public class GameEconomyTests
{
    [UnityTest]
    public IEnumerator DirectLevelLaunchAndSceneChanges_KeepOneWalletAndService()
    {
        yield return SceneManager.LoadSceneAsync("Level");
        yield return null;

        CoinWallet wallet = GameEconomy.Wallet;
        int balance = wallet.Balance;
        AssertSingleService();

        yield return SceneManager.LoadSceneAsync("MainMenu");
        yield return null;
        AssertSingleService();
        Assert.That(GameEconomy.Wallet, Is.SameAs(wallet));

        yield return SceneManager.LoadSceneAsync("Level");
        yield return null;
        AssertSingleService();
        Assert.That(GameEconomy.Wallet, Is.SameAs(wallet));
        Assert.That(GameEconomy.Wallet.Balance, Is.EqualTo(balance));
    }

    private static void AssertSingleService()
    {
        GameEconomy[] services = Object.FindObjectsByType<GameEconomy>();
        Assert.That(services, Has.Length.EqualTo(1));
        Assert.That(services[0].Settings, Is.Not.Null);
        Assert.That(services[0].Settings.HintPrice, Is.EqualTo(5));
    }
}
