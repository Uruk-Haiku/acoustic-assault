using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class CalibrationPanel : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int currentPage;
    public GameObject navigationPanel;
    public List<GameObject> pageList;
    public TMP_Text pageText;
    
    public SelectedObjectManager selectedObjectManager;

    void Update()
    {
        if (navigationPanel.activeSelf == true && EventSystem.current.currentSelectedGameObject == null)
        {
            selectedObjectManager.SetSelectedObject("NextButton");
        }
    }
    public void NextPage()
    {
        pageList[currentPage].SetActive(false);
        if (currentPage < pageList.Count - 1)
        {
            currentPage++;
            pageText.text = currentPage.ToString();
            // If on the last page, hide navigation panel
            if (currentPage == pageList.Count - 1)
            {
                navigationPanel.SetActive(false);
            }
        }
        pageList[currentPage].SetActive(true);
        switch(currentPage)
        {
            case 1:
                selectedObjectManager.SetSelectedObject("NoiseCeiling");
                break;
            case 2:
                selectedObjectManager.SetSelectedObject("NoiseFloor");
                break;
            case 3:
                selectedObjectManager.SetSelectedObject("PitchFloor");
                break;
            case 4:
                selectedObjectManager.SetSelectedObject("PitchCeiling");
                break;
        }
    }

    public void BackPage()
    {
        pageList[currentPage].SetActive(false);
        if (currentPage > 0)
        {
            currentPage--;
            pageText.text = currentPage.ToString();
        }
        pageList[currentPage].SetActive(true);
        switch (currentPage)
        {
            case 0:
                selectedObjectManager.SetSelectedObject("DeviceSelection");
                break;
            case 1:
                selectedObjectManager.SetSelectedObject("NoiseCeiling");
                break;
            case 2:
                selectedObjectManager.SetSelectedObject("NoiseFloor");
                break;
            case 3:
                selectedObjectManager.SetSelectedObject("PitchFloor");
                break;
        }
    }

    void OnEnable()
    {
        currentPage = 0;
        pageText.text = currentPage.ToString();
        pageList.ForEach((panel) =>
        {
            panel.SetActive(false);
            pageText.text = currentPage.ToString();
        });
        navigationPanel.SetActive(true);
        pageList[0].SetActive(true);
    }

    void Start()
    {
    }
}
