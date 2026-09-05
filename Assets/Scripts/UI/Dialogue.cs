using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Dialogue : MonoBehaviour
{
    [SerializeField] private string text;

    [SerializeField] private TMP_Text dialogueText;

    [SerializeField] private float typingSpeed = 0.03f;

    [SerializeField] private InputActionReference skipAction;

    private bool isTyping = false;
    private string fullText = "";
    private Coroutine typingCoroutine;

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
        fullText = text;
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(fullText));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
        typingCoroutine = null;
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
    }
}
