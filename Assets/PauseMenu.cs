using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = true;

    public GameObject pauseMenuUI;

    public Button resumeButton;

    // Update is called once per frame
    void Update()
    {
        if(GameIsPaused){
            Pause();
        }
        else {
            Resume();
        }
    }

    public void Resume(){
        pauseMenuUI.SetActive(false);
        resumeButton.gameObject.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    public void Pause(){
        //pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;
    }
}
