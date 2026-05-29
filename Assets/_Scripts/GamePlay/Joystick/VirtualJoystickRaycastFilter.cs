using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.UI.Input
{
    public class VirtualJoystickRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        [SerializeField] private VirtualJoystick joystick;
        [SerializeField] private RectTransform rootRT;

        [Header("Debug")]
        [SerializeField] private bool logResult = false;

        private void Awake()
        {
            if (joystick == null)
            {
                joystick = GetComponentInParent<VirtualJoystick>();
            }

            if (joystick == null)
            {
                Debug.LogError("[VirtualJoystickRaycastFilter] Joystick missing.");
                enabled = false;
                return;
            }

            if (rootRT == null)
            {
                rootRT = joystick.GetComponent<RectTransform>();
            }

            if (rootRT == null)
            {
                Debug.LogError("[VirtualJoystickRaycastFilter] rootRT missing.");
                enabled = false;
            }
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (joystick.IsTouchLocked)
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRT, sp, eventCamera, out Vector2 local))
            {
                return false;
            }

            bool isValid = joystick.GetAllowedRegionInRoot().Contains(local);

            if (logResult)
            {
                Debug.Log("[VirtualJoystickRaycastFilter] local: " + local + " valid: " + isValid);
            }

            return isValid;
        }
    }
}