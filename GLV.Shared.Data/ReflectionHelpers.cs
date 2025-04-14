using System.Collections;

namespace GLV.Shared.Data;

public static class ReflectionHelpers
{
    /// <summary>
    /// Iterates through the copy's members and clones their cloneable class members. Use this method on a copy of an object that was created using <see cref="object.MemberwiseClone"/>
    /// </summary>
    /// <param name="copy"></param>
    /// <returns></returns>
    public static void ReflectionInnerClone(this ICloneable copy)
    {
        foreach (var prop in copy.GetType().GetProperties().Where(x => x.CanWrite && x.PropertyType.IsClass && x.PropertyType != typeof(string) && x.PropertyType.IsAssignableTo(typeof(ICloneable))))
        {
            var obj = prop.GetValue(copy);
            if (obj is not null)
                prop.SetValue(copy, ((ICloneable)obj).Clone());
        }
    }

    public static Type GetCollectionInnerTypeIfCollection(this Type collectionType)
    {
        if (collectionType.IsAssignableTo(typeof(IEnumerable)))
        {
            var enumerableInterface = collectionType.GetInterfaces().Append(collectionType)
                                                    .Where(x => x.IsConstructedGenericType)
                                                    .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                      ?? throw new InvalidDataException("Collections that don't implement IEnumerable<> are not supported");

            return enumerableInterface.GetGenericArguments()[0];
        }

        return collectionType;
    }

    public static Type GetCollectionInnerType(this Type collectionType)
    {
        if (collectionType.IsAssignableTo(typeof(IEnumerable)))
        {
            var enumerableInterface = collectionType.GetInterfaces().Append(collectionType)
                                                    .Where(x => x.IsConstructedGenericType)
                                                    .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                      ?? throw new InvalidDataException("Collections that don't implement IEnumerable<> are not supported");

            return enumerableInterface.GetGenericArguments()[0];
        }

        throw new ArgumentException("The passed type is not assignable to IEnumerable", nameof(collectionType));
    }
}
