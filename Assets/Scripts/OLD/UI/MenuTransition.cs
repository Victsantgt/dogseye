using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;

public class MenuTransition : MonoBehaviour
{
    public CanvasGroup[] canvasGroup;
    public Image[] image;
    public RectTransform[] buttons;
    public Image vinyl;
    public Image vinylImage;

    private bool gameStarted = false;

    private float offsetX = -300f;
    private float moveDuration = 0.6f;
    private float fadeDuration = 0.4f;
    private float staggerDelay = 0.1f;
    private Ease moveEase = Ease.OutCubic;
    private Ease fadeEase = Ease.OutSine;

    void Awake()
    {
        foreach (Image im in image)
        {
            if (im == null) continue;
            im.gameObject.SetActive(false);
            if (im != image[2]) im.fillAmount = 0;
            else im.material.DOFade(0f, 0).SetEase(Ease.InOutSine);
        }
        foreach (RectTransform b in buttons)
        {
            if (b == null) continue;
            b.gameObject.SetActive(false);
        }
        vinyl.DOFade(0, 0);
        vinylImage.DOFade(0, 0);
    }

    void Update()
    {
        if (gameStarted) return;

        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            gameStarted = true;

            StartCoroutine(Animation());
        }
    }

    IEnumerator Animation()
    {
        foreach (CanvasGroup cg in canvasGroup)
        {
            if (cg == null) continue;

            cg.DOFade(0f, 1).SetEase(Ease.InOutSine);
        }
        yield return new WaitForSeconds(0.2f);
        image[0].gameObject.SetActive(true);
        image[0].DOFillAmount(1, 1).SetEase(Ease.InOutSine);
        image[1].gameObject.SetActive(true);
        image[1].DOFillAmount(1, 1.2f).SetEase(Ease.InOutSine);
        yield return new WaitForSeconds(1f);
        image[2].gameObject.SetActive(true);
        image[2].material.DOFade(1f, 0.5f).SetEase(Ease.InOutSine).OnComplete(() => 
        { 
            vinyl.DOFade(1, 0.3f);
            vinylImage.DOFade(1, 0.3f);
        });

        //Botones
        float delay = 0f;

        foreach (RectTransform btn in buttons)
        {
            if (btn == null) continue;

            btn.gameObject.SetActive(true);

            CanvasGroup cg = btn.GetComponent<CanvasGroup>();

            // Guardamos posición final
            Vector2 finalPos = btn.anchoredPosition;

            // Posición inicial desplazada a la izquierda
            btn.anchoredPosition = finalPos + new Vector2(offsetX, 0f);

            // Estado inicial
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            // Secuencia DOTween
            Sequence seq = DOTween.Sequence();

            seq.AppendInterval(delay);

            seq.Append(
                btn.DOAnchorPos(finalPos, moveDuration)
                   .SetEase(moveEase)
            );

            seq.Join(
                cg.DOFade(1f, fadeDuration)
                  .SetEase(fadeEase)
            );

            seq.OnComplete(() =>
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            });

            delay += staggerDelay;
        }
    }
}
