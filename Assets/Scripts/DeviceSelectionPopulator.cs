using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using System.Linq;
using Lasp;

public class DeviceSelectionPopulator : MonoBehaviour
{
    private TMP_Dropdown deviceDropdown;
    class DeviceItem : TMP_Dropdown.OptionData
    {
        public string id;
        public DeviceItem(in Lasp.DeviceDescriptor device)
          => (text, id) = (device.Name, device.ID);
    }
    void Start()
    {
        // Populate the dropdown
        deviceDropdown = GetComponent<TMP_Dropdown>();
        deviceDropdown.ClearOptions();

        deviceDropdown.options.AddRange
          (Lasp.AudioSystem.InputDevices.Select(dev => new DeviceItem(dev)));

        deviceDropdown.RefreshShownValue();

        // Get dropdown and add listener
        deviceDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }
    public void OnDropdownValueChanged(int value)
    {
        SettingsPanel settingsPanel = GetComponentInParent<SettingsPanel>();
        int playerID = settingsPanel.currentPlayer;

        Debug.Log(settingsPanel.currentPlayer);

        // Get the device ID from the selected dropdown option
        var selectedOption = (DeviceItem)deviceDropdown.options[value];


        // We need to recreate the pitch detector object since you can't change mics when
        // stream is running.
        GameManager.RecreatePitchDetector(playerID);
        SimplePitchDetector pitchDetector = GameManager.GetPitchDetection(playerID);
        Debug.Log("Selecting device ID: " + selectedOption.id);
        pitchDetector.TrySelectDevice(selectedOption.id);
        deviceDropdown.RefreshShownValue();
    }
    
    void OnDestroy()
    {
        // Clean up the listener when destroyed
        deviceDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
    }
}
