using DG.Tweening;
using Patterns.Singleton;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PreviewTextMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image bg;
    public Image text;
    public Image disk;
    public TextMeshProUGUI nowPlay;
    public TextMeshProUGUI hs;
    public AudioClip previewClip;

    private Color hoverColor = Color.white;
    private float fillDuration = 0.2f;

    private Color originalColor;
    private Tween rotationTween;
    private Tween fillTween;
    private Tween textTween;

    void Awake()
    {
        originalColor = bg.color;
        text.DOFillAmount(0, 0);
        nowPlay.DOFade(0, 0);
        hs.DOFade(0, 0);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        bg.DOColor(hoverColor, 0.2f);
        textTween?.Kill();
        textTween = text.DOColor(hoverColor, 0.2f).OnComplete(() =>
            {
                nowPlay.DOFade(1, 0.1f);
                hs.DOFade(1, 0.1f);
            });
        fillTween?.Kill();
        fillTween = text.DOFillAmount(1f, fillDuration);
        rotationTween = disk.rectTransform
            .DOLocalRotate(new Vector3(0, 0, 360), 2f, RotateMode.FastBeyond360)
            .SetRelative(true)
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Linear);
        MusicManager.Instance.PlayPreview(previewClip, 47f);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        bg.DOColor(originalColor, 0.2f);
        text.DOColor(originalColor, 0.2f);
        textTween?.Kill();
        textTween = nowPlay.DOFade(0, 0.1f).OnComplete(() => hs.DOFade(0, 0.1f));
        fillTween?.Kill();
        fillTween = text.DOFillAmount(0f, fillDuration);
        rotationTween?.Kill();
        MusicManager.Instance.StopPreview();
    }
}
