namespace Pedantic.Utilities
{
    public interface IPooledObject<out T>
    {
        public void Clear();
    }
}