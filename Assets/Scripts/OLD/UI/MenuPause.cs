using System;
using Patterns.Singleton;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuPause : MonoBehaviour
{
    public bool musicPaused = false;
    public GameObject pauseScreen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pauseScreen.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!musicPaused) {
                musicPaused = true;
                MusicManager.Instance.PauseMusic();
                pauseScreen.SetActive(true);
                Time.timeScale = 0.0f;
            }
            else
            {
                musicPaused = false;
                MusicManager.Instance.ResumeMusic();
                pauseScreen.SetActive(false);
                Time.timeScale = 1.0f;
            }


        }

    }
}

