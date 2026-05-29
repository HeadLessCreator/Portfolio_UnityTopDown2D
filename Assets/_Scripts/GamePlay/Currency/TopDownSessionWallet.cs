using System;
using System.Collections.Generic;
using UnityEngine;

public class TopDownSessionWallet : MonoBehaviour
{
    [SerializeField] private string[] defaultCurrencyIds = { "Gold" };

    private readonly Dictionary<string, int> sessionCurrencies = new();

    public event Action<string, int> OnSessionCurrencyChanged;

    public void ResetSessionCurrencies()
    {
        sessionCurrencies.Clear();

        foreach (string currencyId in defaultCurrencyIds)
        {
            if (string.IsNullOrWhiteSpace(currencyId))
            {
                continue;
            }

            sessionCurrencies[currencyId] = 0;
            OnSessionCurrencyChanged?.Invoke(currencyId, 0);
        }
    }

    public void AddSessionCurrency(string currencyId, int amount)
    {
        if (string.IsNullOrWhiteSpace(currencyId))
        {
            return;
        }

        if (amount <= 0)
        {
            return;
        }

        int nextAmount = GetSessionCurrency(currencyId) + amount;
        sessionCurrencies[currencyId] = nextAmount;

        OnSessionCurrencyChanged?.Invoke(currencyId, nextAmount);
    }

    public void AddSessionRewards(IEnumerable<TopDownCurrencyReward> rewards)
    {
        if (rewards == null)
        {
            return;
        }

        foreach (TopDownCurrencyReward reward in rewards)
        {
            AddSessionCurrency(reward.currencyId, reward.amount);
        }
    }

    public int GetSessionCurrency(string currencyId)
    {
        if (string.IsNullOrWhiteSpace(currencyId))
        {
            return 0;
        }

        return sessionCurrencies.TryGetValue(currencyId, out int amount)
            ? amount
            : 0;
    }

    public IReadOnlyDictionary<string, int> GetSessionSnapshot()
    {
        return new Dictionary<string, int>(sessionCurrencies);
    }
}