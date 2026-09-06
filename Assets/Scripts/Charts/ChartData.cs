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
            notes[i].time -= noteSpeed;
            notes[i].time += startDelay;
            if (notes[i].lane == "Middle") notes[i].time += 1.9f;
            else notes[i].time += 1f;
        }
    }

    /// <summary>
    /// Suma segundos a todas las notas, sin tocar nada mas.
    ///
    /// Existe porque TransformTimes NO se puede llamar dos veces: ademas del retardo
    /// aplica el ajuste de BPM y de noteSpeed, y a la segunda pasada esos se sumarian
    /// otra vez. El ChartManager carga el chart con retardo 0, arranca la musica, y
    /// solo entonces fija el cero con este metodo, para que el tiempo que tarda la
    /// carga del JSON no cuente como desfase.
    /// </summary>
    public void Desplazar(float segundos)
    {
        if (notes == null) return;

        for (int i = 0; i < notes.Length; i++)
            notes[i].time += segundos;
    }
}
