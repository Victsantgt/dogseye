using Patterns.Observer.Interfaces;
using Patterns.Singleton;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LifeManager : MonoBehaviour, IObserver<NoteHitInfo>
{
    public Transitions transicion;

    // Secuencia de muerte del jugador. La referencia esta viva y ya asignada en la
    // escena; lo unico comentado es la llamada de abajo, en UpdateObserver().
    public PlayerDeathManager muerte;

    public NoteHitSubject subject;
    public float maxLife = 100f;
    public float currentLife = 100f;

    public AudioClip[] noteAudio;

    // Cu�nto suma o resta por nota
    public float lifeGainPerfect = 5f;
    public float lifeGainGood = 2f;
    public float lifeLoseBad = 5f;
    public float lifeLoseMiss = 10f;

    // [ANADIDO: antispam] Lo que cuesta pulsar un carril sin nota. Es la pieza que mata
    // el exploit de machacar las teclas: antes una pulsacion en vacio no costaba NADA,
    // asi que spamear era estrictamente mejor que llevar el ritmo. Deliberadamente mas
    // barato que un Miss: machacar sangra rapido, pero adelantarse una vez sale barato.
    [Tooltip("Vida que se pierde al pulsar la tecla de un carril sin que haya ninguna nota en el.")]
    public float lifeLoseVacio = 3f;

    public Image healthBar;

    private void Start()
    {
        if (subject != null)
            subject.AddObserver(this);

    }

    private void OnDestroy()
    {
        if (subject != null)
            subject.RemoveObserver(this);
    }

    public void UpdateObserver(NoteHitInfo data)
    {
        switch (data.result)
        {
            case HitResult.Perfect:
                currentLife += lifeGainPerfect;
                //MusicManager.Instance.Play_SFX(noteAudio[0]);
                break;
            case HitResult.Good:
                currentLife += lifeGainGood;
                //MusicManager.Instance.Play_SFX(noteAudio[0]);
                break;
            case HitResult.Bad:
                currentLife -= lifeLoseBad;
                MusicManager.Instance.Play_SFX(noteAudio[0]);
                break;
            case HitResult.Miss:
                currentLife -= lifeLoseMiss;
                MusicManager.Instance.Play_SFX(noteAudio[0]);
                break;

            // [ANADIDO: antispam] Pulsacion al aire, sin nota en ese carril. Sin sonido
            // de momento: al machacar sonaria decenas de veces por segundo. Si quereis
            // darle feedback, mejor un clip propio y corto, no el de acierto.
            case HitResult.Vacio:
                currentLife -= lifeLoseVacio;
                break;
        }

        // Limitar la vida entre 0 y maxLife
        currentLife = Mathf.Clamp(currentLife, 0f, maxLife);
        if (currentLife <= 0f)
        {
            transicion.LoseTransition();
        }

    }
}
