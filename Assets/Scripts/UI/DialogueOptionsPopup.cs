using DG.Tweening;
using UnityEngine;

public class DialogueOptionsPopup : MonoBehaviour
{
    [SerializeField] private ImageOption[] options;

    public void ShowOptions(int index)
    {
        if (options == null) return;

        foreach (ImageOption option in options)
        {
            Debug.Log("Opcción creada!");
            if (option == null || option.rectTransform == null)
                continue;

            if (option.textMesh != null && option.texts != null &&
                index >= 0 && index < option.texts.Length)
            {
                option.textMesh.text = option.texts[index];
            }

            if (!option.rectTransform.gameObject.activeSelf)
                option.rectTransform.gameObject.SetActive(true);

            PopOut.pop(option.rectTransform);
        }
    }

    public void HideOptions()
    {
        if (options == null) return;

        foreach (ImageOption option in options)
        {
            if (option == null || option.rectTransform == null)
                continue;

            option.rectTransform.DOKill();

            if (option.rectTransform.gameObject.activeSelf)
                option.rectTransform.gameObject.SetActive(false);
        }
    }
}
