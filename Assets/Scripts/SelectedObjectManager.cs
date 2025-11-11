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
    [SerializeField] private GameObject fs_OcativeSelection;

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
            case "OcativeSelection":
                StartCoroutine(SelectFirstButton(fs_OcativeSelection));
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

        if (selectedButton != fs_MainMenu || selectedButton != fs_SelectLevel || selectedButton != fs_BaseSettings) 
        {
            yield return null;
        }
        else
        {
            // NextButton is on a different panel that takes longer to activate
            yield return new WaitForSeconds(0.1f);
        }
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectedButton);
    }
}