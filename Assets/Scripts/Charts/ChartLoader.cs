using UnityEngine;
using System.IO;

public class ChartLoader : MonoBehaviour
{
    public string filename;

    public ChartData Load()
    {
        string path = Path.Combine(Application.streamingAssetsPath, filename);

        if (!File.Exists(path))
        {
            Debug.LogError("ERROR. NO EXISTE: " + path);
            return null;
        }

        string json = File.ReadAllText(path);
        ChartData chart = JsonUtility.FromJson<ChartData>(json);

        chart.TransformTimes();

        return chart;
    }
}
