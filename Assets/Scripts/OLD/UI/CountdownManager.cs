using DG.Tweening;
using Patterns.Singleton;
using System.Collections;
using TMPro;
using UnityEngine;

public class CountdownManager : MonoBehaviour
{
    private TextMeshProUGUI text;
    private MaterialPropertyBlock mpb;

    public Reveal reveal;
    public Renderer bg;
    public Renderer hit;
    public UISweep life;

    public RectTransform[] letters;

    private float bgValue = 10f;
    private float hitValue = 500f;
    private float popScale = 0.5f;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();

        bg.GetPropertyBlock(mpb);
        mpb.SetFloat("_Fresnel_Power", 10f);
        bg.SetPropertyBlock(mpb);

        hit.GetPropertyBlock(mpb);
        mpb.SetFloat("_x", 500f);
        hit.SetPropertyBlock(mpb);
    }

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        MusicManager.Instance.StopAllCoroutines();
        MusicManager.Instance.StopMusic();
        StartCoroutine(Countdown());
        hit.gameObject.SetActive(false);
        life.gameObject.SetActive(false);
        DOTween.To(
            () => bgValue,
            x =>
            {
                bgValue = x;

                bg.GetPropertyBlock(mpb);
                mpb.SetFloat("_Fresnel_Power", bgValue);
                bg.SetPropertyBlock(mpb);
            },
            1.5f,
            2
        ).SetEase(Ease.OutCubic);
    }

    IEnumerator Countdown()
    {
        float duration = 60f / GameConfig.Instance.GetBPM() * 12f; // Calcular segundos para 1 pulso, y 12 pulsos que dure la animación (que luego realmente se añade uno más al principio)

        yield return new WaitForSeconds(duration / 6);
        text.enabled = true;
        reveal.PlayReveal();
        Pop();
        yield return new WaitForSeconds(duration/2);
        hit.gameObject.SetActive(true);
        DOTween.To(
            () => hitValue,
            x =>
            {
                hitValue = x;

                hit.GetPropertyBlock(mpb);
                mpb.SetFloat("_x", hitValue);
                hit.SetPropertyBlock(mpb);
            },
            4,
            0.8f
        ).SetEase(Ease.OutCubic);
        text.text = "3";
        life.gameObject.SetActive(true);
        life.Sweep();
        Pop();
        yield return new WaitForSeconds(duration/6);
        text.text = "2";
        AnimateLetter(letters[1], 0f);
        AnimateLetter(letters[2], 0f);
        AnimateLetter(letters[0], 0.2f);
        AnimateLetter(letters[3], 0.2f);
        Pop();
        yield return new WaitForSeconds(duration/6);
        text.text = "1";
        Pop();
        yield return new WaitForSeconds(duration/6);
        text.text = "GO!";
        Pop();
        MusicManager.Instance.ReturnToDefault();
        Debug.Log(MusicManager.Instance.getVolume() + ", " + MusicManager.Instance.GetTrack1Playing());
        yield return new WaitForSeconds(duration/6);
        text.enabled = false;
    }

    void Pop()
    {
        text.transform.localScale = Vector3.one;
        text.transform
            .DOScale(popScale, 0)
            .OnComplete(() =>
                text.transform
                    .DOScale(Vector3.one, 0.2f)
                    .SetEase(Ease.InOutBack)
            );
    }

    void AnimateLetter(RectTransform letter, float delay)
    {
        letter.gameObject.SetActive(true);
        letter.localScale = Vector3.zero;

        letter.DOScale(1f, 0.25f)
              .SetDelay(delay)
              .SetEase(Ease.OutBack);
    }
}
