using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using Patterns.Singleton;

public class Transitions : MonoBehaviour
{
    private AudioClip clip;
    private void Start()
    {
        this.transform.DOScale(60, 0).OnComplete(() => 
            this.transform.DOScale(0, 0.5f).SetEase(Ease.OutCirc)
        );
    }

    public void WinTransition()
    {
        MusicManager.Instance.StopMusic();
        this.transform.DOScale(500, 0.5f).SetEase(Ease.InCirc).OnComplete(() =>
            SceneManager.LoadScene("Victoria")
        );
    }

    public void LoseTransition()
    {
        MusicManager.Instance.StopMusic();
        this.transform.DOScale(500, 0.5f).SetEase(Ease.InCirc).OnComplete(() =>
            SceneManager.LoadScene("Derrota")
        );
    }

    public void MenuTransition()
    {
        MusicManager.Instance.StopMusic();
        if (clip != null) MusicManager.Instance.changeDefault(clip);
        this.transform.DOScale(60, 0.5f).SetEase(Ease.InCirc).OnComplete(() =>
            {
                SceneManager.LoadScene("mainMenu");
                MusicManager.Instance.ReturnToDefault();

            }
        );
    }

    public void changeClip(AudioClip newClip) { clip = newClip; }
}
