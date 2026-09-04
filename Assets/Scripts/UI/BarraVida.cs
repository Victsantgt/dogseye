using DG.Tweening;
using Patterns.Singleton;
using UnityEngine;
using UnityEngine.UI;

public class BarraVida : MonoBehaviour
{
    public LifeManager lifeManager;
    public Image relleno;
    public RectTransform barraRoot;

    float lastLife;
    float displayedFill;
    Color baseColor;

    float danger = 0;

    Tween pulseTween;
    float lastPulseIntensity = -1f;

    bool isFlashingDamage;
    bool wasMusicPlaying;

    void Start()
    {
        lastLife = lifeManager.currentLife;
        displayedFill = lifeManager.currentLife / lifeManager.maxLife;
        baseColor = relleno.color;

        wasMusicPlaying = false;
    }

    void Update()
    {
        if (lifeManager == null || relleno == null) return;

        float targetFill = lifeManager.currentLife / lifeManager.maxLife;
        displayedFill = Mathf.Lerp(displayedFill, targetFill, Time.deltaTime * 12f);
        relleno.fillAmount = displayedFill;

        UpdateLowHealthVisuals();

        if (lifeManager.currentLife < lastLife)
            PlayDamageFeedback();

        HandleMusicPulseState();

        lastLife = lifeManager.currentLife;
    }

    void HandleMusicPulseState()
    {
        bool musicPlaying = MusicManager.Instance.IsMusicPlaying();

        if (musicPlaying && !wasMusicPlaying)
        {
            UpdatePulse(true, lastPulseIntensity < 0 ? 1.02f : lastPulseIntensity);
        }
        else if (!musicPlaying && wasMusicPlaying)
        {
            pulseTween?.Kill();
            barraRoot.localScale = Vector3.one;
        }

        wasMusicPlaying = musicPlaying;
    }

    // Baja Vida y Pulso
    void UpdateLowHealthVisuals()
    {
        danger = 1f - displayedFill;

        if (!isFlashingDamage)
        {
            relleno.color = Color.Lerp(baseColor, Color.red, danger);
        }

        float pulseIntensity = Mathf.Lerp(1.02f, 1.10f, danger);

        if (MusicManager.Instance.IsMusicPlaying())
            UpdatePulse(false, pulseIntensity);
    }

    void UpdatePulse(bool force = false, float intensity = 1.02f)
    {
        if (!force && Mathf.Abs(intensity - lastPulseIntensity) < 0.004f)
            return;

        lastPulseIntensity = intensity;

        pulseTween?.Kill();

        float bpm = GameConfig.Instance.GetBPM();
        float beat = 60f / bpm;
        float pulsePeriod = beat * 2f;

        barraRoot.localScale = Vector3.one;

        pulseTween = DOTween.Sequence()
            .Append(barraRoot.DOScale(Vector3.one * intensity, beat * 0.12f).SetEase(Ease.OutQuad))
            .AppendInterval(beat * 0.28f)
            .Append(barraRoot.DOScale(Vector3.one, beat * 0.12f).SetEase(Ease.InQuad))
            .AppendInterval(pulsePeriod - beat * 0.52f)
            .SetLoops(-1);
    }

    // Feedback daño
    void PlayDamageFeedback()
    {
        barraRoot.DOKill(true);
        relleno.DOKill(true);

        float bpm = GameConfig.Instance.GetBPM();
        float beat = 60f / bpm;

        float popTime = beat * 0.12f;
        float returnTime = beat * 0.18f;
        float shakeTime = beat * 0.18f;

        // Pop
        barraRoot
            .DOScale(new Vector3(0.9f, 1.07f, 1f), popTime)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                barraRoot.DOScale(Vector3.one, returnTime).SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        if (MusicManager.Instance.IsMusicPlaying())
                            UpdatePulse(true, lastPulseIntensity);
                    });
            });

        // Flash
        isFlashingDamage = true;
        relleno
            .DOColor(Color.red, popTime)
            .OnComplete(() =>
                relleno.DOColor(Color.Lerp(baseColor, Color.red, danger), returnTime)
                       .OnComplete(() => isFlashingDamage = false)
            );
    }
}
