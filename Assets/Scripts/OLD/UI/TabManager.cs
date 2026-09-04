using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using UnityEngine.Video;
using static UnityEngine.Rendering.DebugUI;

public class TabManager : MonoBehaviour
{
    public GameObject main;
    public GameObject settings;
    public GameObject credits;
    public GameObject confirm;
    public VideoPlayer wifi;
    public VideoPlayer mando;
    public GameObject video;
    public TextMeshProUGUI text;

    public void OpenGame()
    {
        wifi.Play();
        video.SetActive(true);
    }

    public void OpenOptions()
    {
        main.SetActive(false);
        settings.SetActive(true);
        text.DOFade(0f, 0f);
    }

    public void OpenCredits()
    {
        mando.Play();
        video.SetActive(true);
    }

    public void CloseOptions()
    {
        main.SetActive(true);
        settings.SetActive(false);
        confirm.SetActive(false);
    }

    public void ConfirmChanges()
    {
        RectTransform panel = confirm.GetComponent<RectTransform>();
        confirm.SetActive(true);
        panel.localScale = Vector3.zero;

        panel.DOScale(0.8f, 0.25f)
            .SetEase(Ease.OutBack);
    }
    public void DiscardChanges()
    {
        RectTransform panel = confirm.GetComponent<RectTransform>();
        panel.DOScale(0f, 0.25f)
            .SetEase(Ease.InBack)
            .OnComplete(() => confirm.SetActive(false));
    }
    public void ApplyChanges()
    {
        text.DOFade(1f, 0.5f)
            .OnComplete(() =>
            {
                text.DOFade(0f, 1.4f);
            });
    }

}
