using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Portfolio.UI.Input
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Core Components")]
        [SerializeField] private RectTransform bgRT;
        [SerializeField] private RectTransform leverRT;
        [SerializeField] private GameObject guide;

        [Header("Input Settings")]
        [SerializeField, Range(10f, 300f)] private float leverRange = 100f;
        [SerializeField] private bool useInputMagnitudeNormalize = true;

        [Header("Mode")]
        [SerializeField] private JoystickMode joystickMode = JoystickMode.LeftBottom;

        [Header("Region Settings")]
        [SerializeField, Range(0.1f, 0.6f)] private float regionWidthRatio = 0.35f;
        [SerializeField, Range(0.1f, 0.6f)] private float regionHeightRatio = 0.35f;
        [SerializeField, Range(0f, 0.3f)] private float centerBottomYStartRatio = 0.0f;

        [Header("Behavior")]
        [SerializeField] private bool isFloating = true;
        [SerializeField] private bool hideBackgroundOnRelease = true;
        [SerializeField] private bool showGuideWhenIdle = true;

        public event Action<Vector2> OnInputVectorEvent;
        public event Action OnInitialTouchEvent;

        public enum JoystickMode
        {
            LeftBottom,
            RightBottom,
            CenterBottom,
            Free
        }

        public bool IsPressed => isPressed;
        public bool IsTouchLocked => isTouchLock;

        private RectTransform rootRT;

        private bool initialTouch;
        private bool isTouchLock;
        private bool isPressed;

        private int activePointerId = int.MinValue;
        private Vector2 inputVector;

        private void Awake()
        {
            rootRT = GetComponent<RectTransform>();

            if (rootRT == null || bgRT == null || leverRT == null)
            {
                Debug.LogError("[VirtualJoystick] Missing references.");
                enabled = false;
                return;
            }

            ForceIdleState(forceHide: true);
            ApplyDefaultPositionByMode();
        }

        private void OnEnable()
        {
            ForceIdleState(forceHide: true);
            ApplyDefaultPositionByMode();
        }

        private void OnDisable()
        {
            ForceIdleState(forceHide: true);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                ForceIdleState(forceHide: true);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isTouchLock || isPressed)
            {
                return;
            }

            if (!TryGetLocalPointInRoot(eventData, out Vector2 localPosInRoot))
            {
                return;
            }

            if (!GetAllowedRegionInRoot().Contains(localPosInRoot))
            {
                return;
            }

            if (!initialTouch)
            {
                initialTouch = true;
                OnInitialTouchEvent?.Invoke();
            }

            isPressed = true;
            activePointerId = eventData.pointerId;

            if (guide != null)
            {
                guide.SetActive(false);
            }

            bgRT.gameObject.SetActive(true);
            ResetLeverOnly();

            if (isFloating)
            {
                bgRT.anchoredPosition = ClampPointToAllowedRegion(localPosInRoot);
            }

            UpdateLeverFromPointer(localPosInRoot);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsMyPointer(eventData))
            {
                return;
            }

            if (!TryGetLocalPointInRoot(eventData, out Vector2 localPosInRoot))
            {
                return;
            }

            UpdateLeverFromPointer(localPosInRoot);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsMyPointer(eventData))
            {
                return;
            }

            if (!TryGetLocalPointInRoot(eventData, out Vector2 localPosInRoot))
            {
                return;
            }

            UpdateLeverFromPointer(localPosInRoot);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsMyPointer(eventData))
            {
                return;
            }

            ForceIdleState(forceHide: hideBackgroundOnRelease);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsMyPointer(eventData))
            {
                return;
            }

            ForceIdleState(forceHide: hideBackgroundOnRelease);
        }

        public Rect GetAllowedRegionInRoot()
        {
            Rect rect = rootRT.rect;

            if (joystickMode == JoystickMode.Free)
            {
                return rect;
            }

            float w = rect.width * regionWidthRatio;
            float h = rect.height * regionHeightRatio;

            float xMin = rect.xMin;
            float yMin = rect.yMin;

            if (joystickMode == JoystickMode.LeftBottom)
            {
                xMin = rect.xMin;
                yMin = rect.yMin;
            }
            else if (joystickMode == JoystickMode.RightBottom)
            {
                xMin = rect.xMax - w;
                yMin = rect.yMin;
            }
            else if (joystickMode == JoystickMode.CenterBottom)
            {
                xMin = rect.xMin + (rect.width - w) * 0.5f;
                yMin = rect.yMin + rect.height * centerBottomYStartRatio;
            }

            return new Rect(xMin, yMin, w, h);
        }

        private bool IsMyPointer(PointerEventData eventData)
        {
            if (!isPressed)
            {
                return false;
            }

            return eventData.pointerId == activePointerId;
        }

        private bool TryGetLocalPointInRoot(PointerEventData eventData, out Vector2 localPoint)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootRT,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );
        }

        private void UpdateLeverFromPointer(Vector2 pointerLocalPosInRoot)
        {
            Vector2 delta = pointerLocalPosInRoot - bgRT.anchoredPosition;

            Vector2 clampedDelta = delta.magnitude <= leverRange
                ? delta
                : delta.normalized * leverRange;

            leverRT.anchoredPosition = clampedDelta;

            Vector2 raw = clampedDelta / leverRange;

            inputVector = raw == Vector2.zero
                ? Vector2.zero
                : useInputMagnitudeNormalize ? raw.normalized : raw;

            OnInputVectorEvent?.Invoke(inputVector);
        }

        private void ResetLeverOnly()
        {
            leverRT.anchoredPosition = Vector2.zero;
            inputVector = Vector2.zero;
            OnInputVectorEvent?.Invoke(Vector2.zero);
        }

        private void ForceIdleState(bool forceHide)
        {
            isPressed = false;
            activePointerId = int.MinValue;

            ResetLeverOnly();

            if (forceHide)
            {
                bgRT.gameObject.SetActive(false);
            }

            ApplyIdleVisual();

            if (!isFloating)
            {
                ApplyDefaultPositionByMode();
            }
        }

        private void ApplyIdleVisual()
        {
            if (guide != null)
            {
                guide.SetActive(showGuideWhenIdle && !isPressed);
            }

            if (!isPressed && hideBackgroundOnRelease)
            {
                bgRT.gameObject.SetActive(false);
            }
            else
            {
                bgRT.gameObject.SetActive(true);
            }
        }

        private void ApplyDefaultPositionByMode()
        {
            if (isFloating)
            {
                return;
            }

            bgRT.anchoredPosition = GetAllowedRegionInRoot().center;
        }

        private Vector2 ClampPointToAllowedRegion(Vector2 pointInRoot)
        {
            Rect r = GetAllowedRegionInRoot();

            float x = Mathf.Clamp(pointInRoot.x, r.xMin, r.xMax);
            float y = Mathf.Clamp(pointInRoot.y, r.yMin, r.yMax);

            return new Vector2(x, y);
        }
    }
}