using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;

public class Dialogue : MonoBehaviour
{
    [SerializeField] private string[] text;

    private int index = 0;

    [SerializeField] public TMP_Text dialogueText;

    [SerializeField] private float typingSpeed = 0.03f;

    [SerializeField] private InputActionReference skipAction;

    [SerializeField] private DialogueOptionsPopup optionsPopup;

    [SerializeField] private float delayBeforeFinished = 0.3f;

    private bool isTyping = false;
    private string fullText = "";
    private Coroutine typingCoroutine;

    public event Action OnDialogueFinished;

    private void OnEnable()
    {
        if (skipAction != null)
        {
            skipAction.action.Enable();
            skipAction.action.performed += OnSkipPressed;
        }
    }
    private void OnDisable()
    {
        if (skipAction != null)
        {
            skipAction.action.performed -= OnSkipPressed;
            skipAction.action.Disable();
        }
    }

    private void OnSkipPressed(InputAction.CallbackContext context)
    {
        if (isTyping)
        {
            FinishTyping();
        }
    }

    public void StartDialogue()
    {
        fullText = text[index];
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(fullText));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
        typingCoroutine = null;

        FinishTyping();
    }

    private void FinishTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        dialogueText.text = fullText;
        isTyping = false;
        

        OnLineFinished();
    }

    private void OnLineFinished()
    {
        int finishedIndex = index;
        index++;

        if (optionsPopup != null)
            optionsPopup.ShowOptions(finishedIndex);

        StartCoroutine(DialogueFinishedDelay());
    }

    private IEnumerator DialogueFinishedDelay()
    {
        yield return new WaitForSecondsRealtime(delayBeforeFinished);

        OnDialogueFinished?.Invoke();
    }
}
