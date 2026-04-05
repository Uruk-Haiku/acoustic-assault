using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using System.Linq;
using Lasp;
using System.Collections.Generic;

public class DeviceSelectionPopulator : MonoBehaviour
{
    private TMP_Dropdown deviceDropdown;
    class DeviceItem : TMP_Dropdown.OptionData
    {
        public string id;
        public DeviceItem(string displayName, string deviceId)
          => (text, id) = (displayName, deviceId);
    }
    void Awake()
    {
        // Populate the dropdown
        deviceDropdown = GetComponent<TMP_Dropdown>();
        deviceDropdown.ClearOptions();

        // set dropdown value to be currentPlayer device
        SettingsPanel settingsPanel = GetComponentInParent<SettingsPanel>();
        int playerID = settingsPanel.currentPlayer;

        // We need to recreate the pitch detector object since you can't change mics when
        // stream is running.
        GameManager.RecreatePitchDetector(playerID);
        PitchDetector pitchDetector = GameManager.GetPitchDetection(playerID);

        // First pass: count how many times each device name appears
        Dictionary<string, int> totalCounts = new Dictionary<string, int>();
        foreach (var dev in Lasp.AudioSystem.InputDevices)
        {
            if (totalCounts.ContainsKey(dev.Name))
                totalCounts[dev.Name]++;
            else
                totalCounts[dev.Name] = 1;
        }

        // Second pass: build dropdown, appending a number for duplicate names
        Dictionary<string, int> seenCounts = new Dictionary<string, int>();
        List<DeviceItem> deviceItems = new List<DeviceItem>();
        int tempin = 0;

        foreach (var dev in Lasp.AudioSystem.InputDevices)
        {
            string displayName = dev.Name;
            if (totalCounts[dev.Name] > 1)
            {
                if (!seenCounts.ContainsKey(dev.Name))
                    seenCounts[dev.Name] = 0;
                seenCounts[dev.Name]++;
                displayName = $"{dev.Name} ({seenCounts[dev.Name]})";
            }

            if (dev.ID == pitchDetector.deviceID)
            {
                tempin = deviceItems.Count;
            }
            deviceItems.Add(new DeviceItem(displayName, dev.ID));
        }

        deviceDropdown.options.AddRange(deviceItems);
        deviceDropdown.SetValueWithoutNotify(tempin);

        deviceDropdown.RefreshShownValue();


        // Get dropdown and add listener
        deviceDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }
    void OnEnable()
    {
        // Refresh the selected index to match the current player's device
        if (deviceDropdown == null || deviceDropdown.options.Count == 0) return;

        SettingsPanel settingsPanel = GetComponentInParent<SettingsPanel>();
        PitchDetector pitchDetector = GameManager.GetPitchDetection(settingsPanel.currentPlayer);
        if (pitchDetector == null) return;

        int selectedIndex = 0;
        for (int i = 0; i < deviceDropdown.options.Count; i++)
        {
            if (((DeviceItem)deviceDropdown.options[i]).id == pitchDetector.deviceID)
            {
                selectedIndex = i;
                break;
            }
        }
        deviceDropdown.SetValueWithoutNotify(selectedIndex);
        deviceDropdown.RefreshShownValue();
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
        PitchDetector pitchDetector = GameManager.GetPitchDetection(playerID);
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
