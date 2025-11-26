using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIAudioButton : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    [SerializeField] private AudioClip clickClip;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    public void OnPointerClick(PointerEventData eventData)
    {
        SfxManager.Instance?.Play(clickClip, volume);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        // Called for keyboard/controller submit
        SfxManager.Instance?.Play(clickClip, volume);
    }
}