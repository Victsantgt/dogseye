using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Image fadeImage;

    private float fadeDuration = 0.5f;

    private void Start()
    {
        if (fadeImage.gameObject.activeSelf) fadeImage.gameObject.SetActive(false);
    }

    public void StartGame()
    {
        fadeImage.gameObject.SetActive(true);
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
