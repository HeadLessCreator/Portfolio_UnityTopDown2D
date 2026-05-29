using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverPopup : PopupBase
{
    [SerializeField] protected TextMeshProUGUI contentText;

    public override void Open(string title = "", string content = "")
    {
        base.Open(title, content);
    }

    public void ResultSetting(int score, float survivalTime, int earnedGold)
    {
        if (contentText == null)
        {
            return;
        }

        contentText.text =
            $"Score : {score}\n" +
            $"Time : {GameSessionTimer.FormatTime(survivalTime)}\n" +
            $"Gold : {earnedGold}";
    }

    public override void Close()
    {
        HomeButtonOn();
    }

    public void HomeButtonOn()
    {
        GameStateManager.Instance.ChangeState(GameState.Home);
        SoundManager.Instance?.PlayEffect("BUTTON_04");

        base.Close();
    }

    public void RestartButtonOn()
    {
        HomeButtonOn();
    }
}
