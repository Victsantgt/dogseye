using UnityEngine;
using Patterns.Observer.Interfaces;

public class ScoreManager : MonoBehaviour, IObserver<NoteHitInfo>
{
    public NoteHitSubject subject; 
    private int combo = 0;

    private void Start()
    {
        // Registrarse al subject
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
        // Actualizar combo
        if (data.result == HitResult.Miss)
        {
            combo = 0;
        }
        else
        {
            combo++;
        }

        // Calcular score simple
        int baseScore = data.result switch
        {
            HitResult.Perfect => 1000,
            HitResult.Good => 500,
            HitResult.Bad => 200,
            _ => 0
        };

       //score += Mathf.RoundToInt(baseScore * data.accuracy);

       
    }
}
