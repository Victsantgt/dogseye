using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Image fadeImage;

    private AudioSource audio;

    private float fadeDuration = 0.5f;

    private void Start()
    {
        audio = GetComponent<AudioSource>();
        if (fadeImage.gameObject.activeSelf) fadeImage.gameObject.SetActive(false);
    }

    public void StartGame()
    {
        fadeImage.gameObject.SetActive(true);
        audio.DOFade(0, fadeDuration);
        StartCoroutine(TransitionStart());
    }

    private IEnumerator TransitionStart()
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;

            Color c = fadeImage.color;
            c.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
            fadeImage.color = c;

            yield return null;
        }

        SceneManager.LoadScene("Runner");
    }

    public void ExitGame()
    {
        Debug.Log("Saliendo de juego");
        Application.Quit();
    }
}
