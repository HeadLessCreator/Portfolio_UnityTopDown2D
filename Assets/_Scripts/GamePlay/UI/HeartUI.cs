using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartUI : MonoBehaviour
{
    [Header("Heart Root")]
    [SerializeField] private Transform heartRoot;

    [Header("Heart Colors")]
    [SerializeField] private Color activeHeartColor = Color.white;
    [SerializeField] private Color inactiveHeartColor = new Color32(0x20, 0x2C, 0x3D, 0xFF);

    private readonly List<Image> heartImages = new();

    private void Awake()
    {
        CacheHeartImages();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        CacheHeartImages();
    }
#endif

    private void CacheHeartImages()
    {
        heartImages.Clear();

        Transform root = heartRoot != null ? heartRoot : transform;

        for (int i = 0; i < root.childCount; i++)
        {
            Image heartImage = root.GetChild(i).GetComponent<Image>();

            if (heartImage != null)
            {
                heartImages.Add(heartImage);
            }
        }
    }

    public void SetHeart(int currentHeart)
    {
        currentHeart = Mathf.Clamp(currentHeart, 0, heartImages.Count);

        for (int i = 0; i < heartImages.Count; i++)
        {
            bool isActiveHeart = i < currentHeart;
            heartImages[i].color = isActiveHeart ? activeHeartColor : inactiveHeartColor;
        }
    }

    public void Refresh()
    {
        CacheHeartImages();
    }
}