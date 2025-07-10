using GLV.Shared.Data;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;

namespace GLV.Shared.Server.Data;

public static class ModelManipulationHelper
{
    public static bool IsUpdatingNullable<T>(NullUpdateable<T?>? updateNullable, out T? value)
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

    public static bool IsUpdatingNullable<T>(NullUpdateable<T>? updateNullable, out T? value)
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

    public static bool IsUpdatingNullableReference<T>(NullUpdateable<T>? updateNullable, out T? value)
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

        if (DataRegexes.IsEmail().IsMatch(prospectiveEmail))
            return true;

        errors.AddBadEmail(prospectiveEmail);
        return false;
    }

    public static bool IsNull<T>(
        this ref ErrorList errors, 
        [NotNullWhen(false)] T? value, 
        [CallerArgumentExpression(nameof(value))] string property = "")
        where T : struct
    {
        if (value is null)
        {
            errors.RecommendedCode = HttpStatusCode.BadRequest;
            errors.AddError(ErrorMessages.EmptyProperty(property));
            return true;
        }

        return false;
    }

    public static bool IsEmptyString(
        this ref ErrorList errors, 
        [NotNullWhen(false)] string? update,
        [CallerArgumentExpression(nameof(update))] string property = ""
    )
    {
        if (string.IsNullOrWhiteSpace(update))
        {
            errors.RecommendedCode = HttpStatusCode.BadRequest;
            errors.AddError(ErrorMessages.EmptyProperty(property));
            return true;
        }

        return false;
    }

    public static bool IsExactLength(this ref ErrorList errors, string update, int length, [CallerArgumentExpression(nameof(update))] string property = "")
    {
        if (update.Length != length)
        {
            errors.RecommendedCode = HttpStatusCode.BadRequest;
            errors.AddError(ErrorMessages.NotExactLength(property, length, update.Length));
            return false;
        }

        return true;
    }

    public static bool IsTooLong(this ref ErrorList errors, string update, int maxlength,
        [CallerArgumentExpression(nameof(update))] string property = "")
    {
        if (update.Length > maxlength)
        {
            errors.RecommendedCode = HttpStatusCode.BadRequest;
            errors.AddError(ErrorMessages.TooLong(property, maxlength, update.Length));
            return true;
        }

        return false;
    }

    public static bool CheckIfEmail(this FormValidationContext formContext, [NotNullWhen(true)] string? prospectiveEmail,
        [CallerArgumentExpression(nameof(prospectiveEmail))] string property = "")
    {
        if (formContext.IsEmptyString(prospectiveEmail))
            return false;

        if (DataRegexes.IsEmail().IsMatch(prospectiveEmail))
            return true;

        formContext.GetField(property).Errors.AddBadEmail(prospectiveEmail);
        return false;
    }

    public static bool IsNull<T>(
        this FormValidationContext formContext,
        [NotNullWhen(false)] T? value,
        [CallerArgumentExpression(nameof(value))] string property = "")
        where T : struct
    {
        if (value is null)
        {
            formContext.GetField(property).AddError(ErrorMessages.EmptyProperty(property));
            return true;
        }

        return false;
    }

    public static bool IsEmptyString(
        this FormValidationContext formContext,
        [NotNullWhen(false)] string? update,
        [CallerArgumentExpression(nameof(update))] string property = ""
    )
    {
        if (string.IsNullOrWhiteSpace(update))
        {
            formContext.GetField(property).AddError(ErrorMessages.EmptyProperty(property));
            return true;
        }

        return false;
    }

    public static bool IsExactLength(this FormValidationContext formContext, string update, int length, [CallerArgumentExpression(nameof(update))] string property = "")
    {
        if (update.Length != length)
        {
            formContext.GetField(property).AddError(ErrorMessages.NotExactLength(property, length, update.Length));
            return false;
        }

        return true;
    }

    public static bool IsTooLong(this FormValidationContext formContext, string update, int maxlength,
        [CallerArgumentExpression(nameof(update))] string property = "")
    {
        if (update.Length > maxlength)
        {
            formContext.GetField(property).AddError(ErrorMessages.TooLong(property, maxlength, update.Length));
            return true;
        }

        return false;
    }
}
