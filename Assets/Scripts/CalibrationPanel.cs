using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CalibrationPanel : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int currentPage;
    public GameObject navigationPanel;
    public List<GameObject> pageList;
    public TMP_Text pageText;

    public float noiseCeil;

    public float noiseFloor;

    public float pitchCeil;

    public float pitchFloor;

    public void NextPage()
    {
        pageList[currentPage].SetActive(false);
        if (currentPage < pageList.Count - 1)
        {
            currentPage++;
            pageText.text = currentPage.ToString();
        }
        pageList[currentPage].SetActive(true);
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
