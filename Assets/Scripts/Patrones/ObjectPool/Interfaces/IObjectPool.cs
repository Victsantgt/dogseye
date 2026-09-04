namespace Patterns.ObjectPool.Interfaces
{
    public interface IObjectPool
    {
        public IPooleableObject Get();
        public void Release(IPooleableObject obj);
    }
}