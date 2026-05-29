using TMPro;
using UnityEngine;

public class TopDownGameHUD : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI sessionGoldText;

    [Header("Heart")]
    [SerializeField] private HeartUI heartUI;

    [Header("Home HUD")]
    [SerializeField] private TextMeshProUGUI totalGoldText;

    public void SetScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    public void SetTimer(float seconds)
    {
        if (timerText != null)
        {
            timerText.text = GameSessionTimer.FormatTime(seconds);
        }
    }

    public void SetHearts(int currentHeart)
    {
        heartUI?.SetHeart(currentHeart);
    }

    public void SetSessionGold(int amount)
    {
        if (sessionGoldText != null)
        {
            sessionGoldText.text = amount.ToString();
        }
    }

    public void SetTotalGold(int amount)
    {
        if (totalGoldText != null)
        {
            totalGoldText.text = amount.ToString();
        }
    }

    public void SetSessionCurrency(string currencyId, int amount)
    {
        if (currencyId == "Gold")
        {
            SetSessionGold(amount);
        }
    }

    public void ResetHUD(int score, int heart)
    {
        SetScore(score);
        SetTimer(0f);
        SetHearts(heart);
    }
}