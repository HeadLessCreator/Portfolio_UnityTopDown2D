using UnityEngine;
using UnityEngine.EventSystems;

public class PointerTest : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"<color=magenta>PointerDown 작동 성공! 클릭된 오브젝트: {gameObject.name}</color>");
    }
}