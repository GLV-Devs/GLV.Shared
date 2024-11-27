namespace GLV.Shared.Data;

public interface IRepository<TModel, TKey, TView, TCreateModel, TUpdateModel>
    where TModel : class, IKeyed<TModel, TKey>
    where TKey : unmanaged
{
    public ValueTask<SuccessResult<TModel>> Find(TKey id);

    public IQueryable<TModel> Get(IEntityQuery<TModel, TKey>? query = null);

    public IQueryable<TView> GetViews(IQueryable<TModel> entities);

    public IQueryable<TView> GetViewQueryable(TModel model);

    public TView GetView(TModel model);

    public ValueTask<SuccessResult<TView>?> Update(TKey key, TUpdateModel updateModel);

    public ValueTask<SuccessResult> Delete(TModel entity);

    public ValueTask<SuccessResult?> Delete(TKey id);

    public ValueTask<SuccessResult<int>> Delete(IEnumerable<TKey> ids);

    public ValueTask<SuccessResult<int>> Delete(IQueryable<TModel> entities);

    public ValueTask<SuccessResult<TModel>> Create(TCreateModel creationModel);

    public ValueTask<SuccessResult> SaveChanges();
}
