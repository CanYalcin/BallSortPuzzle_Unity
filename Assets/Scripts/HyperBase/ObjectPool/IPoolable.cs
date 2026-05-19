namespace HyperBase.ObjectPool
{
    /// <summary>Implement on any MonoBehaviour that should be pooled.</summary>
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }
}
