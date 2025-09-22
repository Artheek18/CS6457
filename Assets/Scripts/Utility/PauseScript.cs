using UnityEngine;

public class PauserScript : MonoBehaviour
{
    bool isGamePaused = false;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isGamePaused)
            {
                Time.timeScale = 0.5f;
                isGamePaused = false;
            }
            else
            {
                Time.timeScale = 0f;
                isGamePaused = true;
            }
        }
    }
}