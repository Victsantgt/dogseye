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

    private int nextNote = 0;
    private float currentTime = 0;
    private ChartData chart;
    private ChartLoader loader;

    private void Start()
    {
        loader = GetComponent<ChartLoader>();

        NextSection(filename);
    }

    void Update()
    {
        if (!chartActive) return;

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
        currentTime = Time.timeSinceLevelLoad;
        chart = loader.Load(newFilename, currentTime);
        nextNote = 0;
        MusicManager.Instance.SetTimeSinceBegin(currentTime);
        MusicManager.Instance.ReturnToDefault();
        chartActive = true;
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
