namespace GLV.Shared.Data;

public interface IKeyed<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    public TKey Id { get; }
}
