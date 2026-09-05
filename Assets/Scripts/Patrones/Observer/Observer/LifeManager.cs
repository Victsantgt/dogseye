using Patterns.Observer.Interfaces;
using Patterns.Singleton;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LifeManager : MonoBehaviour, IObserver<NoteHitInfo>
{
    //public Transitions transicion;

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
                //MusicManager.Instance.Play_SFX(noteAudio[1]);
                break;
            case HitResult.Bad:
                currentLife -= lifeLoseBad;
                //MusicManager.Instance.Play_SFX(noteAudio[2]);
                break;
            case HitResult.Miss:
                currentLife -= lifeLoseMiss;
                //MusicManager.Instance.Play2D_SFX(noteAudio[3]);
                break;
        }

        // Limitar la vida entre 0 y maxLife
        currentLife = Mathf.Clamp(currentLife, 0f, maxLife);
        if (currentLife <= 0f)
        {
            //transicion.LoseTransition();

            // DESCOMENTA ESTA LINEA cuando el sistema de notas este arreglado y la vida
            // sea fiable. Dispara la misma secuencia de muerte que ahora se prueba a mano
            // con la tecla M (frenado, notas fuera, fundido a negro y R para reiniciar).
            // Al descomentarla conviene poner UsarTeclaDePrueba a false en el jugador.
            //if (muerte != null) muerte.Morir();
        }

    }
}
