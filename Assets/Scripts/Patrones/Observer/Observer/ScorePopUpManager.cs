using UnityEngine;
using TMPro;
using DG.Tweening;
using Patterns.Observer.Interfaces;

public class ScorePopupManager : MonoBehaviour, IObserver<NoteHitInfo>
{
    private Vector3 originalScale;
    public NoteHitSubject subject;
    public TextMeshPro resultText;

    [Header("Colors")]
    public Color perfectColor = Color.yellow;
    public Color goodColor = Color.green;
    public Color badColor = new Color(1f, 0.5f, 0f);
    public Color missColor = Color.red;

    [Header("Tween Settings")]
    public float popScale = 1f;
    public float popDuration = 0.15f;
    public float fadeDelay = 0.20f;
    public float fadeDuration = 0.25f;

    private void Start()
    {
        if (subject != null)
            subject.AddObserver(this);

        resultText.text = "";
        resultText.alpha = 0f;
        originalScale = resultText.transform.localScale;
    }

    private void OnDestroy()
    {
        if (subject != null)
            subject.RemoveObserver(this);
    }

    public void UpdateObserver(NoteHitInfo data)
    {
        DisplayResult(data);
    }

    private void DisplayResult(NoteHitInfo data)
    {
        switch (data.result)
        {
            case HitResult.Perfect:
                resultText.text = "PERFECT";
                resultText.color = perfectColor;
                break;

            case HitResult.Good:
                resultText.text = "GOOD";
                resultText.color = goodColor;
                break;

            case HitResult.Bad:
                resultText.text = "BAD";
                resultText.color = badColor;
                break;

            case HitResult.Miss:
                resultText.text = "MISS";
                resultText.color = missColor;
                break;
        }

        PlayPopupTween();
    }

    private void PlayPopupTween()
    {
        Transform t = resultText.transform;

        // Cancelar tweens previos
        t.DOKill();
        resultText.DOKill();

        // Reiniciar estado 
        t.localScale = originalScale;
        resultText.alpha = 1f;  

        // POP
        t.DOScale(originalScale * (1f + popScale), popDuration)
         .SetEase(Ease.OutQuad)
         .OnComplete(() =>
             t.DOScale(originalScale, popDuration * 0.8f)
              .SetEase(Ease.OutQuad)
         );

        // FADE OUT
        resultText.DOFade(0f, fadeDuration)
            .SetDelay(fadeDelay)
            .SetEase(Ease.OutQuad);
    }
}