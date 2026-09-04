using Patterns.Singleton;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChartManager : MonoBehaviour
{
    public NoteSpawner spawner;
    public Transitions transition;

    private int nextNote = 0;
    
    [SerializeField] private TextMeshProUGUI time;

    void Update()
    {

        ChartLoader loader = GetComponent<ChartLoader>();
        ChartData chart = loader.Load();

        float currentTime = Time.timeSinceLevelLoad;


        if (currentTime >= MusicManager.Instance.GetLength() + (60f / GameConfig.Instance.GetBPM() * 14f))
        {
            transition.WinTransition();
        }

        if (nextNote >= chart.notes.Length) return;

        if (currentTime >= chart.notes[nextNote].time)
        {
            string lane = chart.notes[nextNote].lane;
            spawner.Spawn(lane);
            nextNote++;
        }
    }
}
