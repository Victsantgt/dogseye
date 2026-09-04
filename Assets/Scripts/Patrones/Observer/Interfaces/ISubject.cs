using UnityEngine;

namespace Patterns.Observer.Interfaces
{
    public interface ISubject<T>
    {
        public void AddObserver(IObserver<T> observer);
        public void RemoveObserver(IObserver<T> observer);
        public void NotifyObservers(T data); //push, para saber qué nota ha sido observada
    }
}
