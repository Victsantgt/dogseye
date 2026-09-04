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
        chart = loader.Load(newFilename);
        nextNote = 0;
        currentTime = Time.timeSinceLevelLoad;
        MusicManager.Instance.SetTimeSinceBegin(currentTime);
        MusicManager.Instance.ReturnToDefault();
        chartActive = true;
    }
}
