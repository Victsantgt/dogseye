using DG.Tweening;
using UnityEngine;

public class PopOut
{
    private static float popScale = 1.2f;
    private static float popDuration = 0.2f;

    public static void pop(RectTransform rect)
    {
        rect.transform.localScale = Vector3.zero;
        rect.DOScale(popScale, popDuration).SetEase(Ease.OutBack).OnComplete(() =>
                rect.DOScale(Vector3.one, popDuration * 0.6f).SetEase(Ease.OutBack)
            );
    }
}
