using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Gắn lên một object UI để báo khi con trỏ chuột đi vào/rời khỏi nó (dùng cho tooltip skill).
/// Object cần có Graphic bật Raycast Target (ví dụ Image) thì mới nhận được sự kiện.
/// </summary>
public class Skill_Hover_Target : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Action OnEnter;
    public Action OnExit;

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnExit?.Invoke();
    }
}
