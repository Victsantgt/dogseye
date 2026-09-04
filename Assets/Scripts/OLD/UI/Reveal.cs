using UnityEngine;
using DG.Tweening;

public class Reveal : MonoBehaviour
{
    [SerializeField] Renderer[] renderers;
    [SerializeField] float revealDuration = 0.35f;

    MaterialPropertyBlock mpb;
    float revealValue = 10f;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();

        // Inicialmente ocultos
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetFloat("_Reveal", 10f);
            r.SetPropertyBlock(mpb);
        }
    }

    public void PlayReveal()
    {
        DOTween.To(
            () => revealValue,
            x =>
            {
                revealValue = x;

                foreach (var r in renderers)
                {
                    r.GetPropertyBlock(mpb);
                    mpb.SetFloat("_Reveal", revealValue);
                    r.SetPropertyBlock(mpb);
                }
            },
            -4f,
            revealDuration
        ).SetEase(Ease.OutCubic);
    }
}
