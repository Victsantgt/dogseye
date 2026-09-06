using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class StartingGameFadeOut : MonoBehaviour
{
    [SerializeField] private Image fadeImage;

    private float fadeDuration = 1f;

    private void Start()
    {
        if (!fadeImage.gameObject.activeSelf)
        {
            fadeImage.gameObject.SetActive(true);
        }

        Color c = fadeImage.color;
        c.a = 1f;
        fadeImage.color = c;

        StartCoroutine(TransitionEnd());
    }

    private IEnumerator TransitionEnd()
    {
        float t = 0f;
        Color c = fadeImage.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;
        fadeImage.gameObject.SetActive(false);
    }
}
