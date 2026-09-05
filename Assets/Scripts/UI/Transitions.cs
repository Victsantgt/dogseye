using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using Patterns.Singleton;
using UnityEngine.UI;

public class Transitions : MonoBehaviour
{
    public Image transitionImage;
    public ChartManager chart;
    private int currentSection;

    private void Start()
    {
        transitionImage.transform.DOScale(60, 0).OnComplete(() =>
            transitionImage.transform.DOScale(0, 0.5f).SetEase(Ease.OutCirc)
        );
    }

    public void WinTransition()
    {
        MusicManager.Instance.StopMusic();
        transitionImage.transform.DOScale(500, 0.5f).SetEase(Ease.InCirc).OnComplete(() =>
            SceneManager.LoadScene("Victoria")
        );
    }

    public void LoseTransition()
    {
        MusicManager.Instance.StopMusic();
        transitionImage.transform.DOScale(500, 0.5f).SetEase(Ease.InCirc).OnComplete(() =>
            SceneManager.LoadScene("Derrota")
        );
    }

    public void NextTransition()
    {
        Debug.Log("siguiente parte");
        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(1);
        seq.AppendCallback(() => { chart.NextSection("test2.json"); });
    }
}
