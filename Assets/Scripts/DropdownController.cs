using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class DropdownController : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    public SelectedObjectManager selectedObjectManager;

    private void Awake()
    {
        dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    public void OnDropdownClicked()
    {
        // Wait a frame because TMP spawns its dropdown list after Show() is called
        Debug.Log("Dropdown clicked, waiting to select first option...");
        StartCoroutine(SelectFirstOptionNextFrame());
    }

    private System.Collections.IEnumerator SelectFirstOptionNextFrame()
    {
        yield return null;
        yield return null;
        yield return null;

        // Find the spawned dropdown list
        var list = GameObject.Find("Dropdown List");
        Debug.Log("Dropdown List found: " + (list != null ? list.name : "null"));
        if (list != null)
        {
            var firstButton = list.GetComponentInChildren<UnityEngine.UI.Toggle>();
            if (firstButton != null)
            {
                EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
                Debug.Log("Dropdown option selected: " + firstButton.gameObject.name);
            }
        }
    }

    public void OnValueChanged(int index)
    {
        Debug.Log("Value changed, waiting to select back to dropdown menu");
        StartCoroutine(SelectDropdownMenuNextFrame());
    }

    private System.Collections.IEnumerator SelectDropdownMenuNextFrame()
    {
        yield return null;
        yield return null;

        // Select back to the dropdown to keep navigation consistent
        selectedObjectManager.SetSelectedObject("DeviceSelection");
    }
}
