using UnityEngine;
using UnityEngine.InputSystem;

public class DebugPopupTester : MonoBehaviour
{
    [Header("Debug Key")]
    [SerializeField] private Key debugKey = Key.G;

    [Header("Popup Test")]
    [SerializeField] private int testScore = 1234;
    [SerializeField] private float testTime = 999f;

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[debugKey].wasPressedThisFrame)
        {
            ShowGameOverPopup();
        }
    }

    private void ShowGameOverPopup()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[DebugPopupTester] UIManager.Instance is null.");
            return;
        }

        Debug.Log("[DebugPopupTester] Show GameOverPopup");

        UIManager.Instance.GameOverPopupOnOff(
            true,
            testScore,
            testTime
        );
    }
}