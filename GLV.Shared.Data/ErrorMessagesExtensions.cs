using System.Net;
using System.Runtime.CompilerServices;

namespace GLV.Shared.Data;

public static partial class ErrorMessagesExtensions
{
    public static ref ErrorList AddInvalidCommandToken(this ref ErrorList list)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.InvalidCommandToken());
    }

    public static ref ErrorList AddCommandTokenExpired(this ref ErrorList list)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.CommandTokenExpired());
    }

    public static ref ErrorList AddCommandTokenNotYetAvailable(this ref ErrorList list)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.CommandTokenNotYetAvailable());
    }

    public static ref ErrorList AddEntityNotFound(this ref ErrorList list, string entity, string? query)
    {
        list.RecommendedCode = HttpStatusCode.NotFound;
        return ref list.AddError(ErrorMessages.EntityNotFound(entity, query));
    }

    public static ref ErrorList AddSomeEntitiesNotFound(this ref ErrorList list, string entity, int? entityCount)
    {
        list.RecommendedCode = HttpStatusCode.NotFound;
        return ref list.AddError(ErrorMessages.SomeEntitiesNotFound(entity, entityCount));
    }

    public static ref ErrorList AddPropertiesNotEqual(this ref ErrorList list, string property, string otherProperty)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.PropertiesNotEqual(property, otherProperty));
    }

    public static ref ErrorList AddInvalidServerHttpCode(this ref ErrorList list, HttpStatusCode code)
        => ref list.AddError(ErrorMessages.InvalidServerHttpCode(code));

    public static ref ErrorList AddUnexpectedServerResponse(this ref ErrorList list, int code, string? name = null)
        => ref list.AddError(ErrorMessages.UnexpectedServerResponse(code, name));

    public static ref ErrorList AddEmailAlreadyConfirmed(this ref ErrorList list)
    {
        list.RecommendedCode = HttpStatusCode.Conflict;
        return ref list.AddError(ErrorMessages.EmailAlreadyConfirmed());
    }

    public static ref ErrorList AddVerificationRequestAlreadyActive(this ref ErrorList list)
    {
        list.RecommendedCode = HttpStatusCode.Conflict;
        return ref list.AddError(ErrorMessages.VerificationRequestAlreadyActive());
    }

    public static ref ErrorList AddPersonHasUser(this ref ErrorList list, string query)
    {
        list.RecommendedCode = HttpStatusCode.Conflict;
        return ref list.AddError(ErrorMessages.PersonHasUser(query));
    }

    public static ref ErrorList AddUnknownFileType(this ref ErrorList list, string fileType)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.AddUnknownFileType(fileType));
    }

    public static ref ErrorList AddActionDisallowed(this ref ErrorList list, DisallowableAction? action = null, DisallowableActionTarget? target = null, string? targetName = null)
    {
        list.RecommendedCode = HttpStatusCode.Forbidden;
        return ref list.AddError(ErrorMessages.ActionDisallowed(action, target, targetName));
    }

    public static ref ErrorList AddConfirmationNotSame(this ref ErrorList list, string property)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.ConfirmationNotSame(property));
    }

    public static ref ErrorList AddLoginRequires(this ref ErrorList list, string requirement, string user)
    {
        list.RecommendedCode = HttpStatusCode.Forbidden;
        return ref list.AddError(ErrorMessages.LoginRequires(requirement, user));
    }

    public static ref ErrorList AddLoginLockedOut(this ref ErrorList list, string user)
    {
        list.RecommendedCode = HttpStatusCode.Forbidden;
        return ref list.AddError(ErrorMessages.LoginLockedOut(user));
    }

    public static ref ErrorList AddBadLogin(this ref ErrorList list)
    {
        list.RecommendedCode = HttpStatusCode.Forbidden;
        return ref list.AddError(ErrorMessages.BadLogin());
    }

    public static ref ErrorList AddUserNotFound(this ref ErrorList list, string user)
    {
        list.RecommendedCode = HttpStatusCode.NotFound;
        return ref list.AddError(ErrorMessages.UserNotFound(user));
    }

    public static ref ErrorList AddImageFormatNotSupported(this ref ErrorList list, string? supported = null)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.ImageFormatNotSupported(supported));
    }

    public static ref ErrorList AddClientApplicationError(this ref ErrorList list, string? message = null)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.ClientApplicationError(message));
    }

    public static ref ErrorList AddInternalError(this ref ErrorList list, string? message = null)
    {
        list.RecommendedCode = HttpStatusCode.InternalServerError;
        return ref list.AddError(ErrorMessages.InternalError(message));
    }

    public static ref ErrorList AddEmptyBody(this ref ErrorList list)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.EmptyBody());
    }

    public static ref ErrorList AddBadPhoneNumber(this ref ErrorList list, string phoneNumber)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.BadPhoneNumber(phoneNumber));
    }

    public static ref ErrorList AddBadEmail(this ref ErrorList list, string email)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.BadEmail(email));
    }

    public static ref ErrorList AddBadUsername(this ref ErrorList list, string username)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.BadUsername(username));
    }

    public static ref ErrorList AddInvalidProperty(this ref ErrorList list, string property)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.InvalidProperty(property));
    }

    public static ref ErrorList AddPropertyNotFound(this ref ErrorList list, string property)
    {
        list.RecommendedCode = HttpStatusCode.NotFound;
        return ref list.AddError(ErrorMessages.AddPropertyNotFound(property));
    }

    public static ref ErrorList AddUniqueValueForPropertyAlreadyExists(this ref ErrorList list, string property, string value)
    {
        list.RecommendedCode = HttpStatusCode.Conflict;
        return ref list.AddError(ErrorMessages.UniqueValueForPropertyAlreadyExists(property, value));
    }

    public static ref ErrorList AddUniqueEntityAlreadyExists(this ref ErrorList list, string entity)
    {
        list.RecommendedCode = HttpStatusCode.Conflict;
        return ref list.AddError(ErrorMessages.AddUniqueEntityAlreadyExists(entity));
    }

    public static ref ErrorList AddEmptyProperty(this ref ErrorList list, string? property = null)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.EmptyProperty(property));
    }

    public static ref ErrorList AddBadPassword(this ref ErrorList list)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.BadPassword());
    }

    public static ref ErrorList AddTooLong(this ref ErrorList list, string property, int maxCharacters, int currentCharacters)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.TooLong(property, maxCharacters, currentCharacters));
    }

    public static ref ErrorList AddTimedOut(this ref ErrorList list, string action)
    {
        list.RecommendedCode = HttpStatusCode.RequestTimeout;
        return ref list.AddError(ErrorMessages.TimedOut(action));
    }

    public static ref ErrorList AddNoPermission(this ref ErrorList list)
    {
        list.RecommendedCode = HttpStatusCode.Unauthorized;
        return ref list.AddError(ErrorMessages.NoPermission());
    }

    public static ref ErrorList AddNoPostContent(this ref ErrorList list)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.NoPostContent());
    }

    public static ref ErrorList AddPhoneNumberAlreadyInUse(this ref ErrorList list, string value)
    {
        list.RecommendedCode = HttpStatusCode.Conflict;
        return ref list.AddError(ErrorMessages.PhoneNumberAlreadyInUse(value));
    }

    public static ref ErrorList AddEmailAlreadyInUse(this ref ErrorList list, string value)
    {
        list.RecommendedCode = HttpStatusCode.Conflict;
        return ref list.AddError(ErrorMessages.EmailAlreadyInUse(value));
    }

    public static ref ErrorList AddUsernameAlreadyInUse(this ref ErrorList list, string value)
    {
        list.RecommendedCode = HttpStatusCode.Conflict;
        return ref list.AddError(ErrorMessages.UsernameAlreadyInUse(value));
    }

    public static ref ErrorList AddNotSupported(this ref ErrorList list, string property, string action)
    {
        list.RecommendedCode = HttpStatusCode.NotImplemented;
        return ref list.AddError(ErrorMessages.NotSupported(property, action));
    }

    public static ref ErrorList AddPasswordRequiredUniqueChars(this ref ErrorList list, int uniqueCharCount = 4)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.PasswordRequiredUniqueChars(uniqueCharCount));
    }

    public static ref ErrorList AddPasswordTooLong(this ref ErrorList list, int maximumLength = 100)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.PasswordTooLong(maximumLength));
    }

    public static ref ErrorList AddPasswordTooShort(this ref ErrorList list, int minimumLength = 6)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.PasswordTooShort(minimumLength));
    }

    public static ref ErrorList AddPasswordRequiresLower(this ref ErrorList list)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.PasswordRequiresLower());
    }

    public static ref ErrorList AddPasswordRequiresNonAlphanumeric(this ref ErrorList list)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.PasswordRequiresNonAlphanumeric());
    }

    public static ref ErrorList AddPasswordRequiresUpper(this ref ErrorList list)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.PasswordRequiresUpper());
    }

    public static ref ErrorList AddProfilePictureTooLarge(this ref ErrorList list, int picSize, int max)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.ProfilePictureTooLarge(picSize, max));
    }

    public static ref ErrorList AddProfilePictureTooSmall(this ref ErrorList list, int picSize, int min)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.ProfilePictureTooSmall(picSize, min));
    }

    public static ref ErrorList AddProfilePictureNotSquare(this ref ErrorList list)
    {
        list.RecommendedCode = HttpStatusCode.BadRequest;
        return ref list.AddError(ErrorMessages.ProfilePictureNotSquare());
    }

    //public static ref ErrorList 
}
