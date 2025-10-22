using UnityEngine;
using UnityEngine.EventSystems;

public class SelectedObjectManager : MonoBehaviour
{
    public static SelectedObjectManager Instance { get; private set; }
    [Header("First Selected Buttons")]
    // fs stands for "first selected"
    [SerializeField] private GameObject fs_MainMenu;
    [SerializeField] private GameObject fs_SelectLevel;
    [SerializeField] private GameObject fs_BaseSettings;
    [SerializeField] private GameObject fs_DeviceSelection;
    [SerializeField] private GameObject fs_NoiseCeiling;
    [SerializeField] private GameObject fs_NoiseFloor;
    [SerializeField] private GameObject fs_PitchFloor;
    [SerializeField] private GameObject fs_PitchCeiling;
    [SerializeField] private GameObject fs_NextButton;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
    public void SetSelectedObject(string objectName)
    {
        switch(objectName)
        {
            case "MainMenu":
                StartCoroutine(SelectFirstButton(fs_MainMenu));
                break;
            case "SelectLevel":
                StartCoroutine(SelectFirstButton(fs_SelectLevel));
                break;
            case "BaseSettings":
                StartCoroutine(SelectFirstButton(fs_BaseSettings));
                break;
            case "DeviceSelection":
                StartCoroutine(SelectFirstButton(fs_DeviceSelection));
                break;
            case "NoiseCeiling":
                StartCoroutine(SelectFirstButton(fs_NoiseCeiling));
                break;
            case "NoiseFloor":
                StartCoroutine(SelectFirstButton(fs_NoiseFloor));
                break;
            case "PitchFloor":
                StartCoroutine(SelectFirstButton(fs_PitchFloor));
                break;
            case "PitchCeiling":
                StartCoroutine(SelectFirstButton(fs_PitchCeiling));
                break;
            case "NextButton":
                StartCoroutine(SelectFirstButton(fs_NextButton));
                break;
            default:
                Debug.LogWarning("No matching first selected object found for: " + objectName);
                break;
        }
    }

    private System.Collections.IEnumerator SelectFirstButton(GameObject selectedButton)
    {
        // Wait for a short period to let UIs fully initialize


        Debug.Log("Selecting button: " + selectedButton.name);

        if (selectedButton != fs_NextButton) 
        {
            yield return null;
            yield return null;
        }
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectedButton);
    }
}