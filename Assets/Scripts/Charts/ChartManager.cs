using System.Collections;
using Patterns.Singleton;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChartManager : MonoBehaviour
{
    public NoteSpawner spawner;
    public Transitions transition;

    public string filename;

    public bool chartActive;

    // --- Arranque ---
    // El chart corre en Time.timeSinceLevelLoad y la musica en el hilo de audio, que
    // son dos relojes distintos. Time.timeSinceLevelLoad avanza sumando deltaTime YA
    // RECORTADO por Time.maximumDeltaTime (0.333 s en este proyecto), asi que todo el
    // tiempo real que se pierde en la carga del primer frame es tiempo que el reloj
    // del juego no cuenta y la musica si. Ese hueco se convierte en desfase fijo.
    //
    // Esto no arregla la causa de fondo, solo el arranque: esperamos a que la escena
    // este pintada y estable antes de lanzar nada, y fijamos el cero del chart DESPUES
    // de que la musica haya sonado, no antes.
    [Header("Arranque")]
    [Tooltip("Espera a que la escena haya pintado y los frames se hayan estabilizado antes de arrancar musica y notas. Desmarcalo para volver al comportamiento anterior.")]
    public bool EsperarAQueLaEscenaEsteLista = true;

    [Tooltip("Frames que se dejan pintar antes de empezar a medir. El primero es larguisimo: NotePool instancia 60 notas en su Awake, mas la compilacion de shaders.")]
    public int FramesDeMargen = 2;

    [Tooltip("Se considera que la carga ha terminado cuando un frame baja de estos segundos.")]
    public float UmbralDeFrameEstable = 0.1f;

    [Tooltip("Tope de espera por si los frames nunca bajan del umbral. Pasado esto se arranca igualmente.")]
    public float EsperaMaxima = 3f;

    [Tooltip("Deja en consola cuanto se ha esperado y con que cero ha arrancado el chart. Util para comprobar el ajuste; desmarcalo cuando este afinado.")]
    public bool LogDelArranque = true;

    private int nextNote = 0;
    private float currentTime = 0;
    private ChartData chart;
    private ChartLoader loader;

    private void Start()
    {
        loader = GetComponent<ChartLoader>();

        if (!EsperarAQueLaEscenaEsteLista)
        {
            NextSection(filename);
            return;
        }

        // Hasta que no arranque de verdad no hay chart cargado, asi que el Update no
        // puede correr aunque el flag viniera marcado desde el Inspector.
        chartActive = false;
        StartCoroutine(ArrancarCuandoLaEscenaEsteLista());
    }

    /// <summary>
    /// Deja que la escena termine de cargarse y de pintar antes de arrancar la musica
    /// y el chart. Solo se usa al empezar el nivel: las llamadas a NextSection() de
    /// mitad de partida (las del RhythmSystemToggle al volver del puente) siguen siendo
    /// inmediatas, porque ahi no hay tiron de carga que esperar.
    /// </summary>
    IEnumerator ArrancarCuandoLaEscenaEsteLista()
    {
        for (int i = 0; i < FramesDeMargen; i++)
            yield return new WaitForEndOfFrame();

        // Se mide con realtimeSinceStartup y no con deltaTime a proposito: deltaTime es
        // justo el que viene recortado, asi que no sirve para detectar el tiron.
        float inicio = Time.realtimeSinceStartup;
        float tope = inicio + EsperaMaxima;
        float marca = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup < tope)
        {
            yield return null;

            float ahora = Time.realtimeSinceStartup;
            float duracionDelFrame = ahora - marca;
            marca = ahora;

            if (duracionDelFrame <= UmbralDeFrameEstable)
                break;
        }

        if (LogDelArranque)
            Debug.Log("ChartManager: escena lista tras " + (Time.realtimeSinceStartup - inicio).ToString("F2")
                + " s reales de espera. Arrancando musica y chart.", this);

        NextSection(filename);
    }

    void Update()
    {
        if (!chartActive) return;
        if (chart == null) return;

        currentTime = Time.timeSinceLevelLoad;

        if (currentTime >= MusicManager.Instance.GetLength())
        {
            chartActive = false;
            transition.NextTransition();
        }

        if (nextNote >= chart.notes.Length) return;

        if (currentTime >= chart.notes[nextNote].time)
        {
            string lane = chart.notes[nextNote].lane;
            spawner.Spawn(lane);
            nextNote++;
        }
    }

    public void NextSection(string newFilename)
    {
        // El orden importa. Antes se tomaba el cero, luego se cargaba el JSON y solo
        // despues sonaba la musica: todo lo que tardaba la lectura de disco y el parseo
        // caia entre el cero de las notas y el Play(), y salia como desfase.
        //
        // 1. Cargar primero, con retardo 0. Aqui esta el coste de disco.
        chart = loader.Load(newFilename, 0f);
        nextNote = 0;

        if (chart == null)
        {
            // El loader ya ha dejado el error en consola. Sin chart no arrancamos,
            // que si no el Update peta en la primera nota.
            chartActive = false;
            return;
        }

        // [CAMBIO: fundido de la intermision] ReturnToDefault() ya NO deja la musica
        // sonando al volver: primero funde el tema anterior y solo despues arranca el
        // nuevo. Hay que preguntarle cuanto va a tardar ANTES de llamarla, porque ella
        // misma cambia la respuesta al ponerse en marcha.
        //
        // Sin este retardo el cero del chart quedaria en "ahora" mientras la cancion
        // empieza segundo y medio despues, o sea que todas las notas irian adelantadas.
        // Al arrancar el nivel no hay nada sonando, asi que el retardo es 0 y el cero se
        // queda donde estaba.
        float retardoDeLaMusica = MusicManager.Instance.RetardoAlVolverAlDefault;

        // 2. Arrancar la musica (o su fundido de salida, si habia algo sonando).
        MusicManager.Instance.ReturnToDefault();

        // 3. Y AHORA fijar el cero, en el instante en que la cancion va a empezar.
        currentTime = Time.timeSinceLevelLoad + retardoDeLaMusica;
        chart.Desplazar(currentTime);
        MusicManager.Instance.SetTimeSinceBegin(currentTime);

        chartActive = true;

        if (LogDelArranque)
            Debug.Log("ChartManager: '" + newFilename + "' con cero en t=" + currentTime.ToString("F3")
                + " s (retardo de fundido " + retardoDeLaMusica.ToString("F2") + " s) y "
                + chart.notes.Length + " notas.", this);
    }

    /// <summary>
    /// Adelanta el puntero del chart hasta la primera nota que aun no ha pasado.
    /// Lo llama RhythmSystemToggle al reactivar el sistema despues del puente entre
    /// secciones: sin esto, todas las notas que tocaban durante la pausa saldrian
    /// seguidas de golpe, una por frame.
    /// </summary>
    public void SaltarNotasPasadas()
    {
        if (chart == null || chart.notes == null) return;

        float ahora = Time.timeSinceLevelLoad;
        while (nextNote < chart.notes.Length && chart.notes[nextNote].time < ahora)
            nextNote++;
    }
}
