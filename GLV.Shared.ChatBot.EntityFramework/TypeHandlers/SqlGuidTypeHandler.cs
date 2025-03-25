using Dapper;
using System.Data;

namespace GLV.Shared.ChatBot.EntityFramework.TypeHandlers;

public class SqlNullableGuidTypeHandler : SqlMapper.TypeHandler<Guid?>
{
    public override void SetValue(IDbDataParameter parameter, Guid? guid) 
        => parameter.Value = guid?.ToString();

    public override Guid? Parse(object value) => value switch
    {
        string str => Guid.Parse(str),
        Guid g => g,
        _ => null,
    };
}
