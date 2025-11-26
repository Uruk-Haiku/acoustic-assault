using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class VolumeSettingPanel : MonoBehaviour
{
    public GameObject volumeSettingsPanel;
    public SelectedObjectManager selectedObjectManager;

    private int sliderIndex = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            if (volumeSettingsPanel.activeSelf)
            {
                Debug.Log("Re-selecting Volume Settings Panel");
                switch (sliderIndex)
                {
                    case 0:
                        SelectedObjectManager.Instance.SetSelectedObject("MusicVolume");
                        break;
                    case 1:
                        SelectedObjectManager.Instance.SetSelectedObject("SFXVolume");
                        break;
                }
            }
        }
    }

    public void setSliderIndex(int index)
    {
        sliderIndex = index;
    }
}
