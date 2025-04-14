using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using GLV.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLV.Shared.EntityFramework;

public readonly record struct DbModelBuilderTypeInfo(Type Type, MethodInfo BuilderMethod, Action<EntityTypeBuilder, Type>? EntityTypeBuilderAction = null);

public static class EntityFrameworkDbContextExtensions
{
    public static void BuildModelWithIDbModel(
        this DbContext context,
        ModelBuilder modelBuilder,
        Func<Type, Action<EntityTypeBuilder, Type>?>? entityTypeBuilderActionFactory = null
    )
        => BuildModelWithIDbModel(
            context,
            modelBuilder,
            context.GetType()
                   .GetProperties()
                   .Select(x => x.PropertyType)
                   .Select(x => x.GetCollectionInnerTypeIfCollection())
                   .FilterTypesAndExtractDbModelInfo(entityTypeBuilderActionFactory)
        );

    public static IEnumerable<DbModelBuilderTypeInfo> FilterTypesAndExtractDbModelInfo(
        this IEnumerable<Type> types,
        Func<Type, Action<EntityTypeBuilder, Type>?>? entityTypeBuilderActionFactory = null
    )
        => types
        .Where(x => CheckIfIDbModel(x))
        .Select(x => new DbModelBuilderTypeInfo(
            x,
            x.GetMethod(
                "BuildModel",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.NonPublic
            ) ?? throw new ArgumentException($"The Type {x} was found to implement IDbModel, but no BuildModel method was found", nameof(types)),
            entityTypeBuilderActionFactory?.Invoke(x)
        ));

    public static void BuildModelWithIDbModel(
        this DbContext context, 
        ModelBuilder modelBuilder,
        IEnumerable<DbModelBuilderTypeInfo> types
    )
    {
        ArgumentNullException.ThrowIfNull(types);

        var entityMethod = modelBuilder.GetType()
                                       .GetMethods()
                                       .Where(x => string.Equals(x.Name, "Entity", StringComparison.OrdinalIgnoreCase))
                                       .Where(x => x.IsGenericMethod)
                                       .Where(x => x.GetParameters().Length == 0)
                                       .First();

        Type[] entityMethodArgs = new Type[1];
        object[] buildModelMethodArgs = new object[2];

        foreach (var (type, method, action) in types)
        {
            if (CheckIfIDbModel(type, out var dbModelType) is false)
                throw new ArgumentException($"Type '{type}' does not implement IDbModel", nameof(types));

            if (method is null)
                throw new MissingMethodException($"The Type {type} was found to implement IDbModel, but no BuildModel method was found");

            
            var modelType = dbModelType.GenericTypeArguments[0];

            entityMethodArgs[0] = modelType;

            var constructedEntityTypeBuilder = (EntityTypeBuilder)entityMethod.MakeGenericMethod(entityMethodArgs).Invoke(modelBuilder, null)!;
            buildModelMethodArgs[0] = context;
            buildModelMethodArgs[1] = constructedEntityTypeBuilder!;

            action?.Invoke(constructedEntityTypeBuilder, modelType);
            method.Invoke(null, buildModelMethodArgs);
        }
    }

    public static bool CheckIfIDbModel(Type type, [NotNullWhen(true)] out Type? dbModelType)
    {
        if (type.IsConstructedGenericType 
            && type.GetGenericTypeDefinition() == typeof(IDbModel<,>)
            && type.GetGenericArguments()[0] == type)
        {
            dbModelType = type;
            return true;
        }

        dbModelType = type.GetInterfaces()
                          .Where(x => x.IsConstructedGenericType 
                                      && x.GetGenericTypeDefinition() == typeof(IDbModel<,>)
                                      && x.GetGenericArguments()[0] == type
                          )
                          .FirstOrDefault();

#if DEBUG
        var succ = dbModelType != null;
#endif

        return dbModelType != null;
    }

    public static bool CheckIfIDbModel(Type type)
        => CheckIfIDbModel(type, out _);
}