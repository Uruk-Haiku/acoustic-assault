using UnityEngine;
using UnityEngine.UI;

public class SceneButtonSetup : MonoBehaviour
{
    public Button myButton;
    public string buttonName;
    public string sceneToLoad;

    void Start()
    {
        Button myButton = GameObject.Find(buttonName).GetComponent<Button>();
        //myButton.onClick.AddListener(() => GameManager.Instance.ChangeScene("OtherScene"));
    }

}
