using DG.Tweening;
using UnityEngine;

public class BubbleText : MonoBehaviour
{
    [SerializeField] private Dialogue dialogue;
    [SerializeField] private float delayBeforeDialogue = 0.3f;

    private RectTransform rectTransform;
    private Tween delayTween;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;

        if (dialogue == null)
            dialogue = GetComponentInChildren<Dialogue>(true);
    }

    public void StartBubbleText()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (rectTransform == null)
            rectTransform = (RectTransform)transform;

        PopOut.pop(rectTransform, OnPopOutComplete);
    }

    private void OnPopOutComplete()
    {
        delayTween?.Kill();
        delayTween = DOVirtual.DelayedCall(delayBeforeDialogue, OnDelayComplete);
    }

    private void OnDelayComplete()
    {
        if (dialogue != null)
            dialogue.StartDialogue();
    }

    public void Hide()
    {
        if (rectTransform != null)
            rectTransform.DOKill();

        if (gameObject.activeSelf)
            gameObject.SetActive(false);

        dialogue.dialogueText.text = "";
    }
}
