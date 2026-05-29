using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class FloatingText : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI rewardText;

    [Header("Animation")]
    [SerializeField] private float lifeTime = 0.8f;
    [SerializeField] private float moveUpDistance = 80f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Coroutine playRoutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (rewardText == null)
        {
            rewardText = GetComponentInChildren<TextMeshProUGUI>();
        }

        gameObject.SetActive(false);
    }

    public void Play(string text, Vector2 anchoredPosition)
    {
        if (rewardText != null)
        {
            rewardText.text = text;
        }

        rectTransform.anchoredPosition = anchoredPosition;
        canvasGroup.alpha = 1f;

        gameObject.SetActive(true);

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        float elapsed = 0f;

        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = startPosition + Vector2.up * moveUpDistance;

        while (elapsed < lifeTime)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / lifeTime);

            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
            canvasGroup.alpha = 1f - t;

            yield return null;
        }

        Deactivate();
    }

    public void Deactivate()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
