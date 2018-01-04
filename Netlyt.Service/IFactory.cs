namespace Netlyt.Service
{
    public interface IFactory<T>
    {
        T Create();
    }
}