using UnityEngine;
using UnityEngine.EventSystems;

// Auto-attached by UIMenuSounds — do not manually add to buttons.
public class UIButtonSounds : MonoBehaviour,
    IPointerEnterHandler, IPointerClickHandler,
    ISelectHandler, ISubmitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.Instance?.PlaySFX("UIHover");
    }

    public void OnSelect(BaseEventData eventData)
    {
        AudioManager.Instance?.PlaySFX("UIHover");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Instance?.PlaySFX("UISelect");
    }

    public void OnSubmit(BaseEventData eventData)
    {
        AudioManager.Instance?.PlaySFX("UISelect");
    }
}