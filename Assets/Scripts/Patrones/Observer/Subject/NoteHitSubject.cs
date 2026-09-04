using UnityEngine;
using System.Collections.Generic;
using Patterns.Observer.Interfaces;

public class NoteHitSubject : MonoBehaviour, ISubject<NoteHitInfo>
{
    private List<IObserver<NoteHitInfo>> observers = new List<IObserver<NoteHitInfo>>();

    public void AddObserver(IObserver<NoteHitInfo> observer)
    {
        if (!observers.Contains(observer))
            observers.Add(observer);
    }

    public void RemoveObserver(IObserver<NoteHitInfo> observer)
    {
        if (observers.Contains(observer))
            observers.Remove(observer);
    }

    public void NotifyObservers(NoteHitInfo data)
    {
        foreach (var obs in observers)
        {
            obs.UpdateObserver(data);
           
        }

    }
}
