using DG.Tweening;
using Patterns.Singleton;
using TMPro;
using UnityEngine;

public class LettersPop : MonoBehaviour
{
    private TextMeshProUGUI text;
    private RectTransform rt;
    private float popScale = 1.2f;
    private float duration = 0.08f;

    
    Tween currentTween;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        rt = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (Input.anyKeyDown && MusicManager.Instance.IsMusicPlaying())
        {
            string letter = text.text.ToLower();

            if (Input.GetKeyDown(letter))
            {
                Pop();
            }
        }
    }

    void Pop()
    {
        currentTween?.Kill();

        rt.localScale = Vector3.one;
        rt.DOScale(popScale, duration)
          .SetEase(Ease.OutBack)
          .OnComplete(() => rt.DOScale(1f, duration * 0.6f));
    }
}
