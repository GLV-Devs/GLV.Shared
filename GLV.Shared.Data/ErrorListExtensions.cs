namespace GLV.Shared.Data;

public static class ErrorListExtensions
{
    public static ref ErrorList AddError(this ref ErrorList list, ErrorMessage message)
    {
        if (list.forceEmpty is false)
            (list._errors ??= new()).Add(message);

        return ref list;
    }

    public static ref ErrorList CopyTo(this ref ErrorList list, ref ErrorList other)
    {
        if (list.forceEmpty is false && other.forceEmpty is false && list._errors is not null)
            (other._errors ??= new()).AddRange(list._errors);
        return ref list;
    }

    public static ref ErrorList AddErrorRange(this ref ErrorList list, IEnumerable<ErrorMessage> messages)
    {
        if (list.forceEmpty is false)
            (list._errors ??= new()).AddRange(messages);

        return ref list;
    }

    public static ref ErrorList Clear(this ref ErrorList list)
    {
        list._errors?.Clear();
        return ref list;
    }
}
