using UnityEngine;
using TMPro;
using Patterns.Observer.Interfaces;
using DG.Tweening;
using Patterns.Singleton;

public class ComboManager : MonoBehaviour, IObserver<NoteHitInfo>
{
    public NoteHitSubject subject;
    public TextMeshPro comboText;

    private int combo = 0;
    public int score = 0;

    //animación
    [Header("Tween Settings")]
    public float popScale = 0.5f;      
    public float popDuration = 0.2f;


    public PlayerInfo playerInfo;
    [SerializeField] private DirtyFlag flagManager;

    private void Start()
    {
        if (subject != null)
            subject.AddObserver(this);

        UpdateComboText(false);

        if (PlayerPrefs.HasKey(flagManager.PlayerPrefsKeyName))
        {
            string json = PlayerPrefs.GetString(flagManager.PlayerPrefsKeyName);
            playerInfo = PlayerInfo.CreateFromJSON(json);
        }
        else
        {
            playerInfo = new PlayerInfo();
        }
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

        // Actualizar combo
        if (data.result == HitResult.Miss)
            combo = 0;
        else
        {
            if (data.result == HitResult.Bad) { score += 1000; }
            if(data.result == HitResult.Good) { score += 2500; }
            if (data.result == HitResult.Perfect) { score += 4750; }

            if(score > playerInfo.highscore)
            {
                flagManager.infoflag = true;
            }
            
            combo++;
        }
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
                comboText.transform
                    .DOScale(popScale, popDuration)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                        comboText.transform
                            .DOScale(Vector3.one, popDuration * 0.6f)
                            .SetEase(Ease.OutBack)
                    );
            }
        }
    }
}
