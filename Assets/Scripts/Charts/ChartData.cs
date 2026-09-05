using Patterns.Singleton;
using UnityEngine;

public class ChartData
{
    public NoteData[] notes;

    public void TransformTimes(float startDelay)
    {
        for (int i = 0; i < notes.Length; i++)
        {
            float noteSpeed = GameConfig.Instance.GetNoteSpeed();
            notes[i].time += 60f / GameConfig.Instance.GetBPM() * 14f;
            notes[i].time -= noteSpeed;
            notes[i].time += startDelay;
        }
    }
}
