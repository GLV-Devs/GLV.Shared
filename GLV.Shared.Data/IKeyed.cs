namespace GLV.Shared.Data;

public interface IKeyed<TEntity, TKey>
    where TEntity : class
    where TKey : unmanaged
{
    public TKey Id { get; }
}
