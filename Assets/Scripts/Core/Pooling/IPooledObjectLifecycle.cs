namespace CuteIssac.Core.Pooling
{
    public interface IPooledObjectLifecycle
    {
        void OnPoolSpawned();
        void OnPoolDespawned();
    }
}
