using System.Collections.Generic;
using UnityEngine;

public class TopDownCurrencyStorage : MonoBehaviour
{
    [SerializeField] private string saveKeyPrefix = "TopDownCurrency_";

    public int GetTotalCurrency(string currencyId)
    {
        if (string.IsNullOrWhiteSpace(currencyId))
        {
            return 0;
        }

        return PlayerPrefs.GetInt(GetSaveKey(currencyId), 0);
    }

    public void SetTotalCurrency(string currencyId, int amount)
    {
        if (string.IsNullOrWhiteSpace(currencyId))
        {
            return;
        }

        PlayerPrefs.SetInt(GetSaveKey(currencyId), Mathf.Max(0, amount));
        PlayerPrefs.Save();
    }

    public void AddTotalCurrency(string currencyId, int amount)
    {
        if (string.IsNullOrWhiteSpace(currencyId))
        {
            return;
        }

        if (amount <= 0)
        {
            return;
        }

        int current = GetTotalCurrency(currencyId);
        int next = current + amount;

        SetTotalCurrency(currencyId, next);
    }

    public void AddSessionToTotal(TopDownSessionWallet sessionWallet)
    {
        if (sessionWallet == null)
        {
            return;
        }

        IReadOnlyDictionary<string, int> snapshot = sessionWallet.GetSessionSnapshot();

        foreach (KeyValuePair<string, int> pair in snapshot)
        {
            AddTotalCurrency(pair.Key, pair.Value);
        }
    }

    private string GetSaveKey(string currencyId)
    {
        return $"{saveKeyPrefix}{currencyId}";
    }
}