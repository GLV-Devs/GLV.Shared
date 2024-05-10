using System.Diagnostics;
using System.Reflection;

namespace GLV.CodeGenerators.CRUDModelGenerators.Data;

public class PropertyData
{
    private Type? type;
    private PropertyInfo? property;

    public Type Type { get { Debug.Assert(type is not null); return type; } set => type = value; }
    public PropertyInfo Property { get { Debug.Assert(property is not null); return property; } set => property = value; }
    public PropertyKind PropertyKind { get; set; }
    public bool IsNullable { get; set; }
}
