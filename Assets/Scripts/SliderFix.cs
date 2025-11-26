using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SliderFix : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private Slider slider;
    public SelectedObjectManager selectedObjectManager;
    [SerializeField] private int sliderIndex = 0;

    public VolumeSettingPanel volumeSettingPanel;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        // As Normal
        volumeSettingPanel.setSliderIndex(sliderIndex);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // Prevent Unity from deselecting this slider on Submit
        StartCoroutine(SelectSlider());
    }

    IEnumerator SelectSlider()
    {
        yield return new WaitForSeconds(0.05f);
        Debug.Log("Slected:" + EventSystem.current.currentSelectedGameObject.name);
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            switch (sliderIndex)
            {
                case 0:
                    selectedObjectManager.SetSelectedObject("MusicVolume");
                    break;
                case 1:
                    selectedObjectManager.SetSelectedObject("SFXVolume");
                    break;
            }
        }
    }
}
