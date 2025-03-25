using GLV.Shared.Data;

namespace GLV.Shared.Server.Data;

public static class ErrorListExtensions
{
    public static ref ErrorList AddError(this ref ErrorList list, ErrorMessage message)
    {
        (list._errors ??= new()).Add(message);
        return ref list;
    }

    public static ref ErrorList AddErrorRange(this ref ErrorList list, IEnumerable<ErrorMessage> messages)
    {
        (list._errors ??= new()).AddRange(messages);
        return ref list;
    }

    public static void Clear(this ref ErrorList list)
        => list._errors?.Clear();
}
