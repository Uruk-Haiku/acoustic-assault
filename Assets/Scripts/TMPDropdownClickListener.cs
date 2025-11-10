using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class TMPDropdownClickListener : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    public SelectedObjectManager selectedObjectManager;

    void Start()
    {
        // When the dropdown is clicked, wait one frame to attach listeners
        StartCoroutine(InitAfterFrame());
    }

    IEnumerator InitAfterFrame()
    {
        yield return null; // wait one frame
        var button = dropdown.GetComponentInChildren<Button>();
        if (button != null)
            button.onClick.AddListener(() => StartCoroutine(WatchDropdownItems()));
        else
            Debug.LogWarning("Button not found on dropdown!");
    }
    private IEnumerator WatchDropdownItems()
    {
        // Wait for the dropdown list to be instantiated
        yield return null;

        // TMP_Dropdown creates a GameObject named "TMP Dropdown List" under the Canvas
        GameObject dropdownList = GameObject.Find("TMP Dropdown List");
        if (dropdownList == null) yield break;

        // Attach to each option's toggle
        Toggle[] toggles = dropdownList.GetComponentsInChildren<Toggle>(true);
        for (int i = 0; i < toggles.Length; i++)
        {
            int index = i;
            toggles[i].onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    // Fires even if it's the same value as before
                    string optionText = dropdown.options[index].text;
                    Debug.Log($"Selected option: {optionText} (index {index})");

                    // Do your logic here:
                    OnDropdownOptionSelected(index, optionText);
                }
            });
        }
    }

    private void OnDropdownOptionSelected(int index, string text)
    {
        selectedObjectManager.SetSelectedObject("DeviceSelection");
    }
}