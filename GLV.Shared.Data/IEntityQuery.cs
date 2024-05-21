namespace GLV.Shared.Data;

public interface IEntityQuery<TModel, TKey>
    where TModel : class, IKeyed<TModel, TKey>
    where TKey : unmanaged
{
    public IQueryable<TModel> PerformQuery(IQueryable<TModel> query);
}
