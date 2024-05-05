using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;

namespace GLV.Shared.Data;

public static class ModelManipulationHelper
{
    public static bool IsUpdatingNullable<T>(UpdateNullableStruct<T?>? updateNullable, out T? value)
        where T : struct
    {
        if (updateNullable.HasValue)
        {
            value = updateNullable.Value.Value;
            return true;
        }
        value = default;
        return false;
    }

    public static bool IsUpdatingNullable<T>(UpdateNullableStruct<T>? updateNullable, out T? value)
        where T : struct
    {
        if (updateNullable.HasValue)
        {
            value = updateNullable.Value.Value;
            return true;
        }
        value = default;
        return false;
    }

    public static bool IsUpdatingNullableString<T>(UpdateNullableStruct<T>? updateNullable, out T? value)
    {
        if (updateNullable.HasValue)
        {
            value = updateNullable.Value.Value;
            return true;
        }
        value = default;
        return false;
    }

    public static bool IsExpectedResponse(this ref ErrorList errors, int code, int expected, string? codeName = null)
    {
        if (code != expected)
        {
            errors.AddError(ErrorMessages.UnexpectedServerResponse(code, codeName));
            return false;
        }

        return true;
    }

    public static bool IsExpectedCode(this ref ErrorList errors, HttpStatusCode code, HttpStatusCode expected = HttpStatusCode.OK)
    {
        if (code != expected)
        {
            errors.AddError(ErrorMessages.InvalidServerHttpCode(code));
            return false;
        }

        return true;
    }

    public static bool IsUpdatingString(string? original, [NotNullWhen(true)] string? update, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        => update is not null && string.Equals(original, update, comparison) is false;

    public static bool IsUpdating<T>(T original, [NotNullWhen(true)] T? update)
        => update is not null && EqualityComparer<T>.Default.Equals(original, update) is false;

    public static bool CheckIfEmail(this ref ErrorList errors, [NotNullWhen(true)] string? prospectiveEmail,
        [CallerArgumentExpression(nameof(prospectiveEmail))] string property = "")
    {
        if (errors.IsEmptyString(prospectiveEmail))
            return false;

        if (Regexes.IsEmail().IsMatch(prospectiveEmail))
            return true;

        errors.AddBadEmail(prospectiveEmail);
        return false;
    }

    public static bool IsEmptyString(this ref ErrorList errors, [NotNullWhen(false)] string? update,
        [CallerArgumentExpression(nameof(update))] string property = "")
    {
        if (string.IsNullOrWhiteSpace(update))
        {
            errors.RecommendedCode = HttpStatusCode.BadRequest;
            errors.AddError(ErrorMessages.EmptyProperty(property));
            return true;
        }

        return false;
    }

    public static bool IsTooLong(this ref ErrorList errors, string update, int maxlength, string property)
    {
        if (update.Length > maxlength)
        {
            errors.RecommendedCode = HttpStatusCode.BadRequest;
            errors.AddError(ErrorMessages.TooLong(property, maxlength, update.Length));
            return true;
        }

        return false;
    }
}
