using CastleGuard.App;
using CastleGuard.Economy;
using CastleGuard.Economy.Persistence;
using CastleGuard.Level;
using CastleGuard.Progress;
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CastleGuard.Tests.PlayMode;

public class ProgressTests
{
    [UnityTest]
    public IEnumerator VictoryWithoutNextButton_MenuAndSceneReloadResumeNextLevel()
    {
        string progressKey = $"CastleGuard.Tests.Progress.{Guid.NewGuid():N}";
        string walletKey = $"CastleGuard.Tests.Coins.{Guid.NewGuid():N}";
        FieldInfo progressField = typeof(GameSave).GetField("_progress", BindingFlags.Static | BindingFlags.NonPublic);
        FieldInfo walletField = typeof(GameSave).GetField("_wallet", BindingFlags.Static | BindingFlags.NonPublic);
        PlayerProgress originalProgress = GameSave.Progress;
        CoinWallet originalWallet = GameSave.Wallet;
        progressField.SetValue(null, new PlayerProgress(progressKey));
        walletField.SetValue(null, new CoinWallet(new PlayerPrefsCoinWalletStore(walletKey)));

        try
        {
            yield return SceneManager.LoadSceneAsync("Level");
            yield return null;

            LevelController controller = UnityEngine.Object.FindFirstObjectByType<LevelController>();
            controller.SendMessage("HandleTowerClicked", "B");
            Assert.That(new PlayerProgress(progressKey).LevelNumber, Is.EqualTo(1));
            controller.SendMessage("HandleTowerClicked", "C");
            controller.SendMessage("HandleTowerClicked", "D");
            controller.SendMessage("HandleTowerClicked", "A");
            Assert.That(new PlayerProgress(progressKey).LevelNumber, Is.EqualTo(2));

            progressField.SetValue(null, new PlayerProgress(progressKey));
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;

            ContinueLevelView view = UnityEngine.Object.FindFirstObjectByType<ContinueLevelView>();
            Assert.That(view.GetComponent<TMP_Text>().text, Is.EqualTo("Продолжить с уровня 2"));

            yield return SceneManager.LoadSceneAsync("Level");
            yield return null;

            controller = UnityEngine.Object.FindFirstObjectByType<LevelController>();
            FieldInfo titleField = typeof(LevelController).GetField("levelTitle", BindingFlags.Instance | BindingFlags.NonPublic);
            Text title = (Text)titleField.GetValue(controller);
            Assert.That(title.text, Is.EqualTo("Уровень 2"));
        }
        finally
        {
            progressField.SetValue(null, originalProgress);
            walletField.SetValue(null, originalWallet);
            PlayerPrefs.DeleteKey(progressKey);
            PlayerPrefs.DeleteKey(walletKey);
            PlayerPrefs.Save();
        }
    }
}
