using System.Collections;

namespace GLV.Shared.Data;

public static class ReflectionHelpers
{
    public static Type GetCollectionInnerType(this Type collectionType)
    {
        if (collectionType.IsAssignableTo(typeof(IEnumerable)))
        {
            var enumerableInterface = collectionType.GetInterfaces()
                                                   .Where(x => x.IsConstructedGenericType)
                                                   .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                      ?? throw new InvalidDataException("Collections that don't implement IEnumerable<> are not supported");

            return enumerableInterface.GetGenericArguments()[0];
        }

        throw new ArgumentException("The passed type is not assignable to IEnumerable", nameof(collectionType));
    }
}
