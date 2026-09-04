namespace Patterns.ObjectPool.Interfaces
{
    public interface IPooleableObject : IPrototype
    {
        public bool Active
        {
            get;
            set;
        }
        
        public void Reset();
    }
}