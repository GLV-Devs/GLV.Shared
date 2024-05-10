using System.Text;
using GLV.Shared.Data.Identifiers;
using GLV.Shared.EntityFramework;

namespace GLV.CodeGenerators.CRUDModelGenerators.Generators;

public static class GeneratorHelpers
{
    public static StringBuilder AddTabs(this StringBuilder sb, int tabs)
    {
        for (int i = 0; i < tabs; i++)
            sb.Append('\t');
        return sb;
    }

    public static (string Expression, HashSet<Type> InvolvedTypes) BuildTypeNameAsCSharpTypeExpression(Type type)
    {
        if (type.IsGenericType && type.IsConstructedGenericType is false)
            throw new ArgumentException("This method does not support open generic types", nameof(type));

        HashSet<Type> involvedTypes = [];
        involvedTypes.Add(type);
        bool isValueTuple = type.FullName!.StartsWith(typeof(ValueTuple).FullName!) && type.IsValueType;
        ;

        var sb = new StringBuilder();
        AddTypeNameWithoutGeneric(sb, type.Name);
        FillTypeParams(sb, type.GenericTypeArguments, involvedTypes);
        return (sb.ToString(), involvedTypes);

        static void FillTypeParams(StringBuilder sb, Type[] genericTypes, HashSet<Type> involvedTypes)
        {
            if (genericTypes.Length == 0) return;

            Type type;
            sb.Append('<');
            for (int i = 0; i < genericTypes.Length - 1; i++)
            {
                type = genericTypes[i];
                involvedTypes.Add(type);

                AddTypeNameWithoutGeneric(sb, type.Name);
                FillTypeParams(sb, type.GetGenericArguments(), involvedTypes);
                sb.Append(", ");
            }

            type = genericTypes[^1];
            involvedTypes.Add(type);

            AddTypeNameWithoutGeneric(sb, type.Name);
            FillTypeParams(sb, type.GetGenericArguments(), involvedTypes);
            sb.Append('>');
        }

        static void AddTypeNameWithoutGeneric(StringBuilder sb, string name)
        {
            int ind = name.IndexOf('`');
            if (ind > 0)
                sb.Append(name.AsSpan(0, ind));
            else
                sb.Append(name);
        }
    }

    public static bool CanBeReplacedWithString(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable != null)
            type = nullable;

        return type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(DateTime) || type == typeof(DateTimeOffset);
    }

    public static Type CheckForAndReplaceAsString(Type type)
        => CanBeReplacedWithString(type)
            ? typeof(string)
            : type;

    public static bool IsBasicType(Type type, out bool isNullable)
    {
        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null)
        {
            isNullable = true;
            type = nullable;
        }
        else
            isNullable = false;

        return type == typeof(int)
                || type == typeof(uint)
                || type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(Guid)
                || type == typeof(Snowflake)
                || type == typeof(string)
                || type == typeof(DateOnly)
                || type == typeof(TimeOnly)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal)
                || type == typeof(bool)
                || type.IsAssignableTo(typeof(Enum));
    }

    public static Type? GetIdType(Type type)
        => type == typeof(int)
            || type == typeof(uint)
            || type == typeof(byte)
            || type == typeof(sbyte)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(long)
            || type == typeof(ulong)
            || type == typeof(Guid)
            || type == typeof(Snowflake)
            || type.IsAssignableTo(typeof(Enum))
            ? type
            : (type.GetInterfaces()
                   .Where(x => x.IsConstructedGenericType)
                   .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IDbModel<,>))
                  ?.GetGenericArguments()[1]);
}
