using UnityEngine;
using UnityEngine.EventSystems;

public class MenuUIController : MonoBehaviour
{
    [SerializeField] private GameObject firstSelected;

    void Start()
    {
        StartCoroutine(SelectFirstButtonNextFrame());
    }

    public void SelectStartSong()
    {
        StartCoroutine(SelectFirstButtonNextFrame());
    }

    private System.Collections.IEnumerator SelectFirstButtonNextFrame()
    {
        // Wait one frame to let EventSystem and UI fully initialize
        yield return null;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }
}

