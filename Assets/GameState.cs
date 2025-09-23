using UnityEngine;

public class GameState : MonoBehaviour
{
    // Variable declarations
    [Header("Game State Variables")]
    public int masterState = 0; // 0 = title screen, 1 = level selection, 2 = results screen, 10 = "I Want It That Way"

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        masterState = 0; // Initialize to title screen
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetMasterState(int state)
    {
        masterState = state;
    }

    public int getMasterState()
    {
        return masterState;
    }
}
