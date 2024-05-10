using GLV.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLV.Shared.EntityFramework;

public interface IDbModel<TModel, TKey> : IKeyed<TModel, TKey>
    where TModel : class, IKeyed<TModel, TKey>, IDbModel<TModel, TKey>
    where TKey : unmanaged
{
    public static abstract void BuildModel(DbContext context, EntityTypeBuilder<TModel> mb);
}
