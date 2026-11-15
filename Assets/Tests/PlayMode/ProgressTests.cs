using CastleGuard.App;
using CastleGuard.Economy;
using CastleGuard.Economy.Persistence;
using CastleGuard.Progress;
using System;
using System.Collections;
using System.Reflection;
using CastleGuard.Level.View;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

            LevelView controller = UnityEngine.Object.FindFirstObjectByType<LevelView>();
            Button hintButton = GetButton(controller, "hintButton");
            Assert.That(hintButton.interactable, Is.False);
            controller.enabled = false;
            GameSave.Wallet.Credit(10);
            Assert.That(hintButton.interactable, Is.False);
            controller.enabled = true;
            Assert.That(hintButton.interactable, Is.True);
            hintButton.onClick.Invoke();
            Assert.That(GameSave.Wallet.Balance, Is.Zero);
            Assert.That(hintButton.interactable, Is.False);
            GetButton(controller, "restartButton").onClick.Invoke();
            ClickTower(controller, "B");
            GetButton(controller, "undoButton").onClick.Invoke();
            Assert.That(GetButton(controller, "undoButton").interactable, Is.False);
            ClickTower(controller, "B");
            Assert.That(new PlayerProgress(progressKey).LevelNumber, Is.EqualTo(1));
            ClickTower(controller, "C");
            ClickTower(controller, "D");
            ClickTower(controller, "A");
            Assert.That(new PlayerProgress(progressKey).LevelNumber, Is.EqualTo(2));

            progressField.SetValue(null, new PlayerProgress(progressKey));
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;

            ContinueLevelView view = UnityEngine.Object.FindFirstObjectByType<ContinueLevelView>();
            Assert.That(view.GetComponent<TMP_Text>().text, Is.EqualTo("Продолжить с уровня 2"));

            yield return SceneManager.LoadSceneAsync("Level");
            yield return null;

            controller = UnityEngine.Object.FindFirstObjectByType<LevelView>();
            FieldInfo titleField = typeof(LevelView).GetField("levelTitle", BindingFlags.Instance | BindingFlags.NonPublic);
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

    private static void ClickTower(LevelView view, string towerId)
    {
        TowerView tower = view.transform.Find($"Tower_{towerId}").GetComponent<TowerView>();
        PointerEventData click = new(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
        };
        tower.OnPointerClick(click);
    }

    private static Button GetButton(LevelView view, string fieldName)
    {
        FieldInfo field = typeof(LevelView).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return (Button)field.GetValue(view);
    }
}
