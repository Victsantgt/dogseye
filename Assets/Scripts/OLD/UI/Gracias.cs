using DG.Tweening;
using Patterns.Singleton;
using UnityEngine;

public class Gracias : MonoBehaviour
{
    public RectTransform panel;

    private bool isOpen = false;
    private Tween currentTween;

    void Start()
    {
        panel.localScale = Vector3.zero;
        panel.gameObject.SetActive(false);
        if (GameConfig.Instance.IsMaxCombo())
        {
            Abrir();
        }
    }

    public void Abrir()
    {
        if (currentTween != null) currentTween.Kill();

        panel.gameObject.SetActive(true);
        panel.localScale = Vector3.zero;

        currentTween = panel.DOScale(0.8f, 0.25f)
            .SetEase(Ease.OutBack);

        isOpen = true;
    }

    public void Cerrar()
    {
        if (currentTween != null) currentTween.Kill();

        currentTween = panel.DOScale(0f, 0.25f)
            .SetEase(Ease.InBack)
            .OnComplete(() => panel.gameObject.SetActive(false));

        isOpen = false;
    }
}
