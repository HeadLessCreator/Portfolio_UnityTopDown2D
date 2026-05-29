using System.Collections.Generic;
using UnityEngine;

public class FloatingTextPool : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private FloatingText textPrefab;
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private bool allowPoolExpansion = true;
    [SerializeField] private int maxPoolSize = 50;

    [Header("Canvas")]
    [SerializeField] private RectTransform canvasRoot;
    [SerializeField] private Camera worldCamera;

    [Header("World Offset")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.5f, 0f);

    private readonly List<FloatingText> pool = new();
    private bool isInitialized;

    private void Awake()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        if (isInitialized)
        {
            return;
        }

        if (textPrefab == null)
        {
            Debug.LogError("[FloatingRewardTextPool] textPrefab is missing.");
            return;
        }

        if (canvasRoot == null)
        {
            canvasRoot = GetComponent<RectTransform>();
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateText();
        }

        isInitialized = true;
    }

    public void ShowGold(int amount, Vector3 worldPosition)
    {
        if (amount <= 0)
        {
            return;
        }

        ShowText($"+{amount}G", worldPosition);
    }

    public void ShowText(string text, Vector3 worldPosition)
    {
        InitializePool();

        if (textPrefab == null || canvasRoot == null)
        {
            return;
        }

        FloatingText rewardText = GetInactiveText();

        if (rewardText == null)
        {
            if (!CanExpandPool())
            {
                return;
            }

            rewardText = CreateText();
        }

        Vector3 displayWorldPosition = worldPosition + worldOffset;
        Vector2 anchoredPosition = WorldToCanvasPosition(displayWorldPosition);

        rewardText.Play(text, anchoredPosition);
    }

    private Vector2 WorldToCanvasPosition(Vector3 worldPosition)
    {
        Camera targetCamera = worldCamera != null ? worldCamera : Camera.main;

        Vector3 screenPosition = targetCamera != null
            ? targetCamera.WorldToScreenPoint(worldPosition)
            : worldPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRoot,
            screenPosition,
            null,
            out Vector2 anchoredPosition
        );

        return anchoredPosition;
    }

    private FloatingText GetInactiveText()
    {
        foreach (FloatingText text in pool)
        {
            if (text != null && !text.gameObject.activeSelf)
            {
                return text;
            }
        }

        return null;
    }

    private FloatingText CreateText()
    {
        FloatingText text = Instantiate(
            textPrefab,
            canvasRoot != null ? canvasRoot : transform
        );

        text.gameObject.SetActive(false);
        pool.Add(text);

        return text;
    }

    private bool CanExpandPool()
    {
        return allowPoolExpansion && pool.Count < maxPoolSize;
    }

    public void ClearAll()
    {
        foreach (FloatingText text in pool)
        {
            if (text != null)
            {
                text.Deactivate();
            }
        }
    }
}