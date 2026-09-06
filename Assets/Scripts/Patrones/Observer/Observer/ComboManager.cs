using UnityEngine;
using TMPro;
using Patterns.Observer.Interfaces;
using DG.Tweening;
using Patterns.Singleton;
using UnityEngine.UI;

public class ComboManager : MonoBehaviour, IObserver<NoteHitInfo>
{
    public NoteHitSubject subject;
    public TextMeshProUGUI comboText;

    private int combo = 0;
    public int score = 0;

    public Image image;
    public RectTransform rct;

    public Sprite perfect;
    public Sprite good;
    public Sprite bad;
    public Sprite miss;

    //animación
    [Header("Tween Settings")]
    public float popScale = 0.5f;      
    public float popDuration = 0.2f;

    private Sequence seq;
    private Sequence seqImage;

    private void Start()
    {
        if (subject != null)
            subject.AddObserver(this);

        rct.DOScale(0, 0);

        UpdateComboText(false);
    }

    private void OnDestroy()
    {
        if (subject != null)
            subject.RemoveObserver(this);
    }

    public void UpdateObserver(NoteHitInfo data)
    {
        //switch (data.result)
        //{
        //    case HitResult.Perfect:
        //    case HitResult.Good:
        //    case HitResult.Bad:
        //        combo++;
        //        break;

        //    case HitResult.Miss:
        //        combo = 0;
        //        break;
        //}

        //UpdateComboText();
        int oldCombo = combo;

        // [CAMBIO: el combo se rompe al fallar] Antes esto solo se reiniciaba con Miss y
        // TODO lo demas caia en el else y sumaba combo. Eso dejaba dos agujeros:
        //
        //   - Un Bad contaba como acierto de combo. Bad es "has llegado, pero raspado":
        //     ya no quita vida (ver LifeManager.lifeLoseBad), pero tampoco puede valer
        //     para sostener una racha, o el combo deja de medir precision.
        //
        //   - HitResult.Vacio, que es pulsar la tecla de un carril SIN nota, tambien
        //     entraba por el else y hacia combo++. O sea que machacar las teclas subia el
        //     combo hasta el infinito sin tocar una sola nota, que es justo el exploit
        //     que el antispam vino a tapar en ColliderNoteScript.
        //
        // Ahora se escribe con un switch para que cada resultado tenga su caso a la
        // vista: si manana se anade otro a HitResult, no se cuela solo en el "todo lo
        // demas suma".
        switch (data.result)
        {
            case HitResult.Perfect:
                image.sprite = perfect;
                score += 4750;
                combo++;
                break;

            case HitResult.Good:
                image.sprite = good;
                score += 2500;
                combo++;
                break;

            // El Bad sigue puntuando: le has dado. Lo que ya no hace es mantener la
            // racha. Si algun dia se quiere que tampoco de puntos, quita esta linea.
            case HitResult.Bad:
                image.sprite = bad;
                score += 1000;
                combo++;
                break;

            case HitResult.Miss:
                image.sprite = miss;
                combo = 0;
                break;

            case HitResult.Vacio:
                image.sprite = miss;
                combo = 0;
                break;
        }

        seqImage.Kill();

        seqImage = DOTween.Sequence();

        seqImage.Append(rct.DOScale(0, 0));
        seqImage.Append(rct.DOScale(1, 0.4f).SetEase(Ease.OutBack));
        seqImage.AppendInterval(0.3f);
        seqImage.Append(rct.DOScale(0, 0.2f).SetEase(Ease.OutBack));

        // Solo animar si sube
        bool shouldAnimate = combo > oldCombo;

        UpdateComboText(shouldAnimate);
    }

    private void UpdateComboText(bool animate)
    {
        //if (comboText == null) return;

        //if (combo <= 0)
        //{
        //    comboText.text = "";
        //}
        //else
        //{
        //    comboText.text = combo.ToString();
        //}
        if (comboText == null) return;

        if (combo <= 0)
        {
            comboText.text = "";
        }
        else
        {
            comboText.text = combo.ToString();

            if (animate)
            {
                // Cancelar tweens previos
                comboText.transform.DOKill();

                // Hacer pop
                comboText.transform.localScale = Vector3.one;

                seq.Kill();
                seq = DOTween.Sequence();

                seq.Append(comboText.transform
                    .DOScale(popScale, popDuration)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                        comboText.transform
                            .DOScale(Vector3.one, popDuration * 0.6f)
                            .SetEase(Ease.OutBack)
                    ));
                seq.AppendInterval(2);
                seq.Append(comboText.transform.DOScale(0, 0.2f).SetEase(Ease.OutBack));
            }
        }
    }
}
