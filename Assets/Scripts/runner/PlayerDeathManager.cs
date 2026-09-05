using System.Collections;
using System.Collections.Generic;
using Patterns.Singleton;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Secuencia de muerte del jugador.
///
/// TEMPORAL: de momento se dispara con la tecla M, porque el sistema de notas todavia
/// no da una vida fiable. Cuando lo este, en LifeManager hay una llamada comentada a
/// Morir() lista para descomentar, y la tecla se puede quitar poniendo UsarTeclaDePrueba
/// en false.
///
/// Al morir: se para el jugador poco a poco (y con el todo lo que lleva encima, porque
/// el sistema de ritmo cuelga de el), se apagan las notas, se funde a negro y sale el
/// texto de derrota. La R reinicia usando exactamente la misma logica que el final de
/// partida ganado.
/// </summary>
[DisallowMultipleComponent]
public class PlayerDeathManager : MonoBehaviour
{
    [Header("Disparo de prueba")]
    [Tooltip("TEMPORAL: permite matar al jugador con una tecla mientras el sistema de notas no sea fiable. Ponlo en false cuando la vida dispare la muerte por si sola.")]
    public bool UsarTeclaDePrueba = true;

    public Key TeclaDeMuerte = Key.M;

    /// <summary>Que hacer con la musica al morir.</summary>
    public enum ModoMusica
    {
        FundirSalida,   // se va apagando mientras el jugador frena
        CortarDeGolpe,  // silencio inmediato
        NoTocarla       // sigue sonando, como estaba antes
    }

    [Header("Musica al morir")]
    [Tooltip("FundirSalida acompana al frenado y suena mejor. CortarDeGolpe la calla al instante.")]
    public ModoMusica MusicaAlMorir = ModoMusica.FundirSalida;

    [Header("Frenado")]
    [Tooltip("Segundos que tarda el jugador en pararse del todo.")]
    public float SegundosDeFrenado = 1.5f;

    [Tooltip("Forma del frenado. Eje Y: 1 = velocidad normal, 0 = parado.")]
    public AnimationCurve CurvaDeFrenado = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Fundido a negro")]
    [Tooltip("Objeto que contiene el negro y el texto. Se activa al morir.")]
    public GameObject PanelMuerte;

    [Tooltip("Imagen negra a pantalla completa.")]
    public Image Negro;

    [Tooltip("Grupo con el texto, para que aparezca despues del negro.")]
    public CanvasGroup GrupoTexto;

    [Tooltip("Segundos que tarda la pantalla en ponerse negra. Empieza cuando el jugador ya se ha parado.")]
    public float DuracionFundido = 1.2f;

    [Tooltip("Segundos que tarda el texto en aparecer, ya sobre el negro.")]
    public float DuracionFundidoTexto = 0.6f;

    [Header("Texto")]
    public TextMeshProUGUI TextoMuerteUI;

    [TextArea]
    public string TextoMuerte = "Has perdido, pulsa R para empezar de nuevo";

    [Header("Reinicio")]
    [Tooltip("Se reutiliza su Reiniciar(), el mismo que usa el final de partida: para la musica del singleton y recarga la escena.")]
    public GameEndManager Reinicio;

    public Key TeclaReinicio = Key.R;

    [Header("Referencias del jugador")]
    public BasicMovement Movimiento;
    public TransitionRush Aceleron;
    public DollyZoomEffect Camara;
    public RhythmSystemToggle SistemaNotas;

    [Header("Que se apaga al morir")]
    [Tooltip("Componentes que se desactivan nada mas morir. OJO: BasicMovement no va aqui, se apaga solo despues de frenar.")]
    public List<Behaviour> ComponentesADesactivar = new List<Behaviour>();

    [Tooltip("Objetos enteros que se desactivan nada mas morir.")]
    public List<GameObject> ObjetosADesactivar = new List<GameObject>();

    bool muerto;
    bool aceptaReinicio;

    /// <summary>True desde que arranca la secuencia de muerte.</summary>
    public bool Muerto { get { return muerto; } }

    void Awake()
    {
        if (Movimiento == null) Movimiento = GetComponent<BasicMovement>();
        if (Aceleron == null) Aceleron = GetComponent<TransitionRush>();

        if (PanelMuerte != null)
            PanelMuerte.SetActive(false);
    }

    void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        if (aceptaReinicio && teclado[TeclaReinicio].wasPressedThisFrame)
        {
            Reiniciar();
            return;
        }

        if (UsarTeclaDePrueba && !muerto && teclado[TeclaDeMuerte].wasPressedThisFrame)
            Morir();
    }

    /// <summary>
    /// Arranca la secuencia de muerte. Es la que hay que llamar desde LifeManager
    /// cuando la vida llegue a 0.
    /// </summary>
    [ContextMenu("Morir (prueba)")]
    public void Morir()
    {
        if (muerto) return;

        // Si la partida ya ha terminado bien, no tiene sentido morir encima.
        if (Reinicio != null && Reinicio.Terminado) return;

        muerto = true;

        ApagarMusica();

        // Las notas fuera ya, o seguirian saliendo mientras el jugador frena.
        // Desactivar() las devuelve al pool, no se quedan congeladas en el aire.
        if (SistemaNotas != null)
            SistemaNotas.Desactivar();

        // El aceleron escribe el multiplicador cada frame: si lo dejamos vivo pelearia
        // con el frenado. Lo cancelamos y lo apagamos antes de tomar el control.
        if (Aceleron != null)
        {
            Aceleron.Cancelar();
            Aceleron.enabled = false;
        }

        if (Camara != null)
        {
            Camara.CancelarManual();
            Camara.enabled = false;   // al desactivarse devuelve la camara a su sitio
        }

        for (int i = 0; i < ComponentesADesactivar.Count; i++)
            if (ComponentesADesactivar[i] != null)
                ComponentesADesactivar[i].enabled = false;

        for (int i = 0; i < ObjetosADesactivar.Count; i++)
            if (ObjetosADesactivar[i] != null)
                ObjetosADesactivar[i].SetActive(false);

        StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        yield return Frenar();

        // Cuando la pantalla empieza a irse a negro tiene que haber silencio si o si,
        // aunque el fundido de la musica se haya quedado a medias por lo que sea.
        if (MusicaAlMorir != ModoMusica.NoTocarla)
            CallarDelTodo();

        yield return FundirANegro();

        aceptaReinicio = true;
    }

    void ApagarMusica()
    {
        if (MusicaAlMorir == ModoMusica.NoTocarla) return;

        MusicManager musica = MusicManager.Instance;
        if (musica == null) return;

        if (MusicaAlMorir == ModoMusica.FundirSalida)
        {
            // La corrutina tiene que correr en el propio MusicManager: es un singleton
            // con DontDestroyOnLoad, y si la lanzaramos desde aqui moriria con nosotros.
            musica.StartCoroutine(musica.FadeOut());
        }
        else
        {
            CallarDelTodo();
        }
    }

    void CallarDelTodo()
    {
        MusicManager musica = MusicManager.Instance;
        if (musica == null) return;

        musica.StopMusic();

        // StopMusic solo para la pista que el manager cree activa. Si hubiera un cambio
        // de pista a medias, la otra seguiria sonando, asi que paramos las dos a mano.
        // Los SFX no se ven afectados: Play_SFX los instancia sueltos, no aqui debajo.
        AudioSource[] fuentes = musica.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < fuentes.Length; i++)
            if (fuentes[i] != null)
                fuentes[i].Stop();
    }

    IEnumerator Frenar()
    {
        if (Movimiento == null)
            yield break;

        float t = 0f;
        while (t < SegundosDeFrenado)
        {
            t += Time.deltaTime;
            float p = SegundosDeFrenado <= 0f ? 1f : Mathf.Clamp01(t / SegundosDeFrenado);
            Movimiento.SetMultiplicador(Mathf.Max(0f, CurvaDeFrenado.Evaluate(p)));
            yield return null;
        }

        Movimiento.SetMultiplicador(0f);
        Movimiento.enabled = false;
    }

    IEnumerator FundirANegro()
    {
        if (PanelMuerte != null)
            PanelMuerte.SetActive(true);

        if (TextoMuerteUI != null)
            TextoMuerteUI.text = TextoMuerte;

        if (GrupoTexto != null)
            GrupoTexto.alpha = 0f;

        if (Negro != null)
        {
            Color c = Negro.color;
            c.a = 0f;
            Negro.color = c;

            float t = 0f;
            while (t < DuracionFundido)
            {
                t += Time.deltaTime;
                c.a = DuracionFundido <= 0f ? 1f : Mathf.Clamp01(t / DuracionFundido);
                Negro.color = c;
                yield return null;
            }

            c.a = 1f;
            Negro.color = c;
        }

        if (GrupoTexto != null)
        {
            float t2 = 0f;
            while (t2 < DuracionFundidoTexto)
            {
                t2 += Time.deltaTime;
                GrupoTexto.alpha = DuracionFundidoTexto <= 0f ? 1f : Mathf.Clamp01(t2 / DuracionFundidoTexto);
                yield return null;
            }

            GrupoTexto.alpha = 1f;
        }
    }

    /// <summary>Reinicia la partida. Publica por si quieres un boton de UI.</summary>
    public void Reiniciar()
    {
        // Reutilizamos el mismo reinicio del final ganado en vez de duplicarlo:
        // para la musica del singleton (que sobrevive a la recarga) y recarga la escena.
        if (Reinicio != null)
        {
            Reinicio.Reiniciar();
            return;
        }

        Debug.LogError("PlayerDeathManager: falta la referencia al GameEndManager, que es quien sabe reiniciar.", this);
    }
}
