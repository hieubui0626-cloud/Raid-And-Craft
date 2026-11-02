using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Gắn lên một object UI có Graphic bật Raycast Target để báo click và con trỏ vào/ra (dùng cho vùng lưới Inventory).
/// </summary>
public class UI_Pointer_Relay : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public event Action<PointerEventData> OnClick;
    public event Action OnEnter;
    public event Action OnExit;

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick?.Invoke(eventData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnExit?.Invoke();
    }
}
