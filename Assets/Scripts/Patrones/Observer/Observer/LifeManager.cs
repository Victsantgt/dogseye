using Patterns.Observer.Interfaces;
using Patterns.Singleton;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LifeManager : MonoBehaviour, IObserver<NoteHitInfo>
{
    public Transitions transicion;

    // [ANADIDO: secuencia de derrota] Quien lleva el tropiezo del carrito y el cambio a
    // la escena de derrota. Si se deja vacio se cae de vuelta al LoseTransition de antes.
    [Tooltip("Secuencia de derrota del Player: tropiezo, caida, fundido y cambio de escena.")]
    public SecuenciaDeDerrota derrota;

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
    // [CAMBIO: Bad neutro] Va a 0 A PROPOSITO, no es un valor a medio poner.
    //
    // Bad no cuenta como fallo para la animacion: Note.OnPlayerHit da por bueno todo lo
    // que no sea Miss, asi que un Bad lanza igualmente la animacion de acierto (la salida
    // despedida de la central, el salto a la cesta de las laterales). Con este valor por
    // encima de 0, el jugador veia que habia acertado y a la vez perdia vida, que es
    // justo la contradiccion que se quiso quitar.
    //
    // Ahora Bad no da ni quita: es el "has llegado, pero raspado". Si algun dia se
    // quiere que vuelva a castigar, basta con subir este numero.
    [Tooltip("Vida que se pierde al acertar dentro de la banda Bad. En 0 a proposito: Bad lanza la animacion de acierto, asi que quitar vida ahi contradice lo que ve el jugador.")]
    public float lifeLoseBad = 0f;
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
            // [CAMBIO: secuencia de derrota] Antes esto llamaba directo a
            // transicion.LoseTransition(), que cerraba la imagen y saltaba de golpe a la
            // escena de derrota. Ahora primero se ve el tropiezo: la camara se queda
            // clavada, las manos desaparecen y el carrito se va solo y se cae. La propia
            // SecuenciaDeDerrota cambia de escena al terminar.
            //
            // Lanzar() es reentrante: con el antispam siguen llegando notas y pulsaciones
            // durante los frames posteriores a morir, y cada una vuelve a entrar aqui.
            // Solo cuenta la primera.
            if (derrota != null)
                derrota.Lanzar();
            else if (transicion != null)
                transicion.LoseTransition();   // por si alguien quita el componente
        }

    }
}
