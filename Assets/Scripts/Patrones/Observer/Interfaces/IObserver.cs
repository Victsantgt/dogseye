using UnityEngine;

namespace Patterns.Observer.Interfaces
{
    public interface IObserver<T>
    {
        public void UpdateObserver(T data);
    }
}
