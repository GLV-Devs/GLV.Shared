namespace GLV.Shared.Data;

public interface IEntityQuery<TModel, TKey>
    where TModel : class, IKeyed<TModel, TKey>
    where TKey : notnull
{
    public IQueryable<TModel> PerformQuery(IQueryable<TModel> query);
}
