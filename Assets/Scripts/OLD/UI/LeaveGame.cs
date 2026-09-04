using DG.Tweening;
using UnityEngine;

public class LeaveGame : MonoBehaviour
{
    public RectTransform panel;
    public GameObject credits;

    private bool isOpen = false;
    private Tween currentTween;

    void Start()
    {
        panel.localScale = Vector3.zero;
        panel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isOpen && !credits.activeInHierarchy)
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

    public void Salir()
    {
        Application.Quit();
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
