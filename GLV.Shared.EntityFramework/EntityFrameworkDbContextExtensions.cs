using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace GLV.Shared.EntityFramework;

public static class EntityFrameworkDbContextExtensions
{
    public static void BuildModelWithIDbModel(this DbContext context, ModelBuilder modelBuilder)
    {
        if (context.Database.IsSqlite())
            modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");
        // This sets the entire DB to be case insensitive

        // From this point on, all this code is just to get the type of each DbSet property to configure itself through IDbModelBuilder

        var entityMethod = modelBuilder.GetType()
                                       .GetMethods()
                                       .Where(x => string.Equals(x.Name, "Entity", StringComparison.OrdinalIgnoreCase))
                                       .Where(x => x.IsGenericMethod)
                                       .Where(x => x.GetParameters().Length == 0)
                                       .First();

        Type[] entityMethodArgs = new Type[1];

        foreach (var (prop, method) in context.GetType()
            .GetProperties()
            .Where(x =>
            {
                var type = x.PropertyType;
                return type.IsConstructedGenericType
                    && type.GetGenericTypeDefinition() == typeof(DbSet<>)
                    && type.GenericTypeArguments[0].GetInterfaces().Any(CheckIfIDbModel);
            })
            .Select(x => (x, x.PropertyType.GenericTypeArguments[0].GetMethod("BuildModel", BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.NonPublic))!))
        {
            if (method is null)
                throw new MissingMethodException($"The property {prop} was found to implement IDbModel, but no BuildModel method was found");

            var modelType = prop.PropertyType.GenericTypeArguments[0]
                .GetInterfaces()
                .FirstOrDefault(CheckIfIDbModel)!
                .GenericTypeArguments[0];

            entityMethodArgs[0] = modelType;
            method.Invoke(null, [context, entityMethod.MakeGenericMethod(entityMethodArgs).Invoke(modelBuilder, null)]);
        }
    }

    private static bool CheckIfIDbModel(Type type)
        => type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IDbModel<,>);
}