using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class TMPDropdownClickListener : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    public SelectedObjectManager selectedObjectManager;



    // Controls which dropdown to select after dropdown is closed (0 for device selection, 1 for ocative selection)
    [SerializeField] private int dropdownIndex;

    void Start()
    {
        // When the dropdown is clicked, wait one frame to attach listeners
        StartCoroutine(InitAfterFrame());
        //StartCoroutine(WatchDropdownItems());
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

        // TMP_Dropdown creates a GameObject named "Dropdown List" under the Canvas
        Transform dropdownList = null;
        while (dropdownList == null)
        {
            dropdownList = dropdown.transform.root.Find("Dropdown List");
            yield return null;
        }
        Debug.Log("Dropdown List found: " + (dropdownList != null ? dropdownList.name : "null"));
        if (dropdownList == null) 
        { 
            Debug.LogWarning("Dropdown List not found!");
            yield break;
        }

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
        if (dropdownIndex == 0)
            SelectedObjectManager.Instance.SetSelectedObject("DeviceSelection");
        else if (dropdownIndex == 1)
            SelectedObjectManager.Instance.SetSelectedObject("OcativeSelection");
    }

    public void OnSubmit()
    {
        // Logic for when the dropdown is submitted (if needed)
        Debug.Log("Dropdown submitted, watching items...");
        StartCoroutine(WatchDropdownItems());
    }
}