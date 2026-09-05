using DG.Tweening;
using System;
using UnityEngine;

public class PopOut
{
    private static float popScale = 1.4f;
    private static float popDuration = 0.7f;

    public static void pop(RectTransform rect, Action onComplete = null)
    {
        rect.DOKill();
        rect.localScale = Vector3.zero;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(rect.DOScale(popScale, popDuration).SetEase(Ease.OutBack));
        sequence.Append(rect.DOScale(Vector3.one, popDuration * 0.6f).SetEase(Ease.OutBack));
        sequence.OnComplete(() => onComplete?.Invoke());
    }
}
