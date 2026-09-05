using System.Collections;
using UnityEngine;

public class PhoneVibration : MonoBehaviour
{
    [SerializeField] private float angle = 15f;
    [SerializeField] private float duration = 0.08f; 
    [SerializeField] private float pause = 0.4f;

    [SerializeField] private BubbleText bubbleText;

    private Coroutine routine;

    public void PlayShake()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShakeSequence());
    }

    private IEnumerator ShakeSequence()
    {
        yield return StartCoroutine(DoRotationSet());
        yield return new WaitForSeconds(pause);
        yield return StartCoroutine(DoRotationSet());
        routine = null;

        if (bubbleText != null)
            bubbleText.StartBubbleText();
    }

    private IEnumerator DoRotationSet()
    {
        // izquierda, derecha, izquierda
        yield return StartCoroutine(RotateTo(-angle));
        yield return StartCoroutine(RotateTo(angle));
        yield return StartCoroutine(RotateTo(-angle));
        yield return StartCoroutine(RotateTo(0f));
    }

    private IEnumerator RotateTo(float targetZ)
    {
        float startZ = transform.localEulerAngles.z;
        if (startZ > 180f) startZ -= 360f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float z = Mathf.Lerp(startZ, targetZ, t);
            transform.localEulerAngles = new Vector3(0, 0, z);
            yield return null;
        }
        transform.localEulerAngles = new Vector3(0, 0, targetZ);
    }

    public void Hide()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;

            gameObject.SetActive(false);
        }
    }
}
