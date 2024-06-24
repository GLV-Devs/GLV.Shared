using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace GLV.Shared.Data;

public static partial class ErrorMessages
{
    public static ErrorMessage InvalidCommandToken()
    => new(
            $"El token de comando es inválido",
            nameof(InvalidCommandToken),
            null
        );

    public static ErrorMessage CommandTokenNotYetAvailable()
    => new(
            $"El token de comando aún no está disponible para ser utilizado",
            nameof(CommandTokenNotYetAvailable),
            null
        );

    public static ErrorMessage CommandTokenExpired()
    => new(
            $"El token de comando está expirado",
            nameof(CommandTokenExpired),
            null
        );

    public static ErrorMessage Unspecified()
    => new(
            $"No produjo un error no especificado",
            nameof(Unspecified),
            null
        );

    public static ErrorMessage AddUnknownFileType(string fileType)
        => new(
            $"No se reconoce el tipo de archivo \"{fileType}\"",
            nameof(AddUnknownFileType),
            new ErrorMessageProperty[]
            {
                new(nameof(fileType), fileType)
            }
        );

    public static ErrorMessage PersonHasUser(string id)
        => new(
            $"La persona {id} posee un usuario. Por favor utilice el controlador de Cuentas.",
            nameof(PersonHasUser),
            new ErrorMessageProperty[]
            {
                new(nameof(id), id)
            }
        );

    public static ErrorMessage SomeEntitiesNotFound(string entity, int? missingEntityCount)
        => new(
            missingEntityCount is not null ? $"{missingEntityCount} de las entidades '{entity}' no pudieron ser encontradas" : $"Algunas de las entidades '{entity}' no pudieron ser encontradas",
            nameof(SomeEntitiesNotFound),
            new ErrorMessageProperty[]
            {
                new(nameof(entity), entity),
                new(nameof(missingEntityCount), missingEntityCount?.ToString())
            }
        );

    public static ErrorMessage EntityNotFound(string entity, string? query)
        => new(
            string.IsNullOrWhiteSpace(query) ? $"No se pudo encontrar la entidad {entity} especificada" : $"No se pudo encontrar una entidad {entity} bajo {query}",
            nameof(EntityNotFound),
            new ErrorMessageProperty[]
            {
                new(nameof(entity), entity),
                new(nameof(query), query ?? "")
            }
        );

    public static ErrorMessage PropertiesNotEqual(string property, string otherProperty)
        => new(
            $"{property} no es igual a {otherProperty}",
            nameof(PropertiesNotEqual),
            new ErrorMessageProperty[] 
            { 
                new(nameof(property), property),
                new(nameof(otherProperty), otherProperty)
            }
        );

    public static ErrorMessage InvalidServerHttpCode(HttpStatusCode code)
        => new(
            $"El servidor envió un código de respuesta inesperado: {(int)code} {Enum.GetName(code)}",
            nameof(InvalidServerHttpCode),
            new ErrorMessageProperty[] { new("code", ((int)code).ToString()) }
        );

    public static ErrorMessage UnexpectedServerResponse(int code, string? name = null)
        => new(
            $"El servidor envió una respuesta inesperada: {code} {name}",
            nameof(UnexpectedServerResponse),
            new ErrorMessageProperty[] { new("code", ((int)code).ToString()) }
        );

    public static ErrorMessage EmailAlreadyConfirmed()
        => new(
            $"El usuario ya verificó su correo electrónico",
            nameof(EmailAlreadyConfirmed),
            null
        );

    public static ErrorMessage VerificationRequestAlreadyActive()
        => new(
            $"El usuario ya posee una verificación de correo activa, y aun no puede pedir otra",
            nameof(VerificationRequestAlreadyActive),
            null
        );

    public static ErrorMessage ActionDisallowed(DisallowableAction? action = null, DisallowableActionTarget? target = null, string? targetName = null)
        => new(
            action is null 
                ? "La accion no está permitida para este usuario" 
                : target is null
                    ? $"La accion '{action}' no está permitida para este usuario"
                    : targetName is null
                        ? $"La accion '{action}' sobre {target} no está permitida para este usuario"
                        : $"La accion '{action}' sobre {target} '{targetName}' no está permitida para este usuario",
            nameof(ActionDisallowed),
            new ErrorMessageProperty[]
            {
                new(nameof(action), action?.ToString()),
                new(nameof(target), target?.ToString()),
                new(nameof(targetName), targetName)
            }
        );

    public static ErrorMessage ConfirmationNotSame(string property)
        => new(
            $"El campo de confirmación para la propiedad '{property}' no coincide con la misma",
            nameof(ConfirmationNotSame),
            new ErrorMessageProperty[]
            {
                new(nameof(property), property)
            }
        );

    public static ErrorMessage LoginRequires(string requirement, string user)
        => new(
            $"Iniciar sesión como el usuario {user} requiere {requirement}",
            nameof(LoginRequires),
            new ErrorMessageProperty[]
            {
                new(nameof(requirement), requirement),
                new(nameof(user), user)
            }
        );

    public static ErrorMessage LoginLockedOut(string user)
        => new(
            $"El usuario {user} se encuentra actualmente bloqueado",
            nameof(LoginLockedOut),
            new ErrorMessageProperty[]
            {
                new(nameof(user), user)
            }
        );

    public static ErrorMessage BadLogin()
        => new(
            "Las credenciales son inválidas",
            nameof(BadLogin),
            null
        );

    public static ErrorMessage UserNotFound(string user)
        => new(
            $"No se encontró el usuario: {user}",
            nameof(UserNotFound),
            new ErrorMessageProperty[]
            {
                new(nameof(user), user)
            }
        );

    public static ErrorMessage ImageFormatNotSupported(string? supported = null)
        => new(
            string.IsNullOrWhiteSpace(supported) ? "El tipo de imagen no está soportado." : $"El tipo de imagen no está soportado. Se cuenta con soporte para: {supported}",
            nameof(ImageFormatNotSupported),
            new ErrorMessageProperty[] { new(nameof(supported), supported ?? "") }
        );

    public static ErrorMessage ClientApplicationError(string? message = null)
        => new(
            string.IsNullOrWhiteSpace(message) ? "Ocurrió un error por parte de la aplicación cliente" : $"Ocurrió un error por parte de la aplicación cliente: {message}",
            nameof(ClientApplicationError),
            string.IsNullOrWhiteSpace(message)
            ? null
            : new ErrorMessageProperty[] { new(nameof(message), message) }
        );

    public static ErrorMessage InternalError(string? message = null)
        => new(
            string.IsNullOrWhiteSpace(message) ? "Ocurrió un error interno en el servidor" : $"Ocurrió un error interno en el servidor: {message}",
            nameof(InternalError),
            string.IsNullOrWhiteSpace(message)
            ? null
            : new ErrorMessageProperty[] { new(nameof(message), message) }
        );

    public static ErrorMessage EmptyBody()
        => new(
            "El cuerpo de la petición está vacio",
            nameof(EmptyBody),
            null
        );
    
    public static ErrorMessage BadPhoneNumber(string phoneNumber)
        => new(
            $"El número de telefono está malformado: {phoneNumber}",
            nameof(BadPhoneNumber),
            new ErrorMessageProperty[]
            {
                new(nameof(phoneNumber), phoneNumber)
            }
        );

    public static ErrorMessage BadEmail(string email)
        => new(
            $"El correo electrónico está malformado: {email}",
            nameof(BadEmail),
            new ErrorMessageProperty[]
            {
                new(nameof(email), email)
            }
        );

    public static ErrorMessage BadUsername(string username)
        => new(
            $"El nombre de usuario no es válido: {username}",
            nameof(BadUsername),
            new ErrorMessageProperty[]
            {
                new(nameof(username), username)
            }
        );

    public static ErrorMessage AddPropertyNotFound(string property)
        => new(
            $"No se encontró la propiedad: {property}",
            nameof(AddPropertyNotFound),
            new ErrorMessageProperty[]
            {
                new(nameof(property), property)
            }
        );

    public static ErrorMessage InvalidProperty(string property)
        => new(
            $"La propiedad es inválida: {property}",
            nameof(InvalidProperty),
            new ErrorMessageProperty[]
            {
                new(nameof(property), property)
            }
        );

    public static ErrorMessage AddUniqueEntityAlreadyExists(string? entity)
        => new(
            string.IsNullOrWhiteSpace(entity) ? $"Esta entidad ya existe" : $"Una entidad de tipo {entity} ya existe con los parametros suministrados",
            nameof(AddUniqueEntityAlreadyExists),
            new ErrorMessageProperty[]
            {
                new(nameof(entity), entity)
            }
        );

    public static ErrorMessage UniqueValueForPropertyAlreadyExists(string property, string? value)
        => new(
            string.IsNullOrWhiteSpace(value) 
                ? $"La propiedad {property} ha de tener un valor único"
                : $"La propiedad {property} ha de tener un valor único, y ya existe una entidad con un valor de {value}",
            nameof(UniqueValueForPropertyAlreadyExists),
            new ErrorMessageProperty[] 
            {
                new(nameof(property), property),
                new(nameof(value), value)
            }
        );

    public static ErrorMessage EmptyProperty(string? property = null)
        => new(
            string.IsNullOrWhiteSpace(property) ? "La propiedad no puede permanecer vacía" : $"La propiedad no puede permanecer vacía: {property}",
            nameof(EmptyProperty),
            string.IsNullOrWhiteSpace(property) ? null : new ErrorMessageProperty[] { new(nameof(property), property) }
        );

    public static ErrorMessage BadPassword()
        => new(
            "La contraseña ingresada es inválida",
            nameof(BadPassword),
            null
        );

    public static ErrorMessage TooLong(string property, int maxCharacters, int currentCharacters)
    {
        var mc = maxCharacters.ToString();
        var cc = currentCharacters.ToString();
        return new(
                $"La propiedad {property} tiene demasiados caracteres ({cc}). Esta propiedad tiene un maximo de {mc} caracteres",
                nameof(TooLong),
                new ErrorMessageProperty[]
                {
                new(nameof(property), property),
                new(nameof(maxCharacters), mc),
                new(nameof(currentCharacters), cc)
                }
            );
    }

    public static ErrorMessage TimedOut(string action)
        => new(
            $"La acción expiró y ya no está disponible: {action}",
            nameof(TimedOut),
            new ErrorMessageProperty[]
            {
                new(nameof(action), action)
            }
        );

    public static ErrorMessage NoPermission()
        => new(
            "El usuario no tiene permisos para realizar ésta acción",
            nameof(NoPermission),
            null
        );

    public static ErrorMessage NoPostContent()
        => new(
            "La petición no cuenta con contenido en el post",
            nameof(NoPostContent),
            null
        );

    public static ErrorMessage PhoneNumberAlreadyInUse(string value)
        => new(
            $"El número de telefono no puede ser '{value}', debido a que ya esta siendo utilizada por otro usuario.",
            nameof(PhoneNumberAlreadyInUse),
            new ErrorMessageProperty[]
            {
                new(nameof(value), value)
            }
        );

    public static ErrorMessage EmailAlreadyInUse(string value)
        => new(
            $"El correo electrónico no puede ser '{value}', debido a que ya esta siendo utilizada por otro usuario.",
            nameof(EmailAlreadyInUse),
            new ErrorMessageProperty[]
            {
                new(nameof(value), value)
            }
        );

    public static ErrorMessage UsernameAlreadyInUse(string value)
        => new(
            $"El nombre de usuario no puede ser '{value}', debido a que ya esta siendo utilizada por otro usuario.",
            nameof(UsernameAlreadyInUse),
            new ErrorMessageProperty[]
            {
                new(nameof(value), value)
            }
        );

    public static ErrorMessage NotSupported(string property, string action)
        => new(
            $"{action} {property} no es soportado en estos momentos",
            nameof(NotSupported),
            new ErrorMessageProperty[]
            {
                new(nameof(property), property),
                new(nameof(action), action)
            }
        );

    public static ErrorMessage PasswordRequiredUniqueChars(int uniqueCharCount = 4)
        => new(
            $"La contraseña debe contener al menos {uniqueCharCount} caracteres únicos",
            nameof(PasswordTooShort),
            new ErrorMessageProperty[]
            {
                new(nameof(uniqueCharCount), uniqueCharCount.ToString())
            }
        );

    public static ErrorMessage PasswordTooLong(int maximumLength = 100)
        => new(
            $"La contraseña debe contener {maximumLength} o menos caracteres",
            nameof(PasswordTooLong),
            new ErrorMessageProperty[]
            {
                new(nameof(maximumLength), maximumLength.ToString())
            }
        );

    public static ErrorMessage PasswordTooShort(int minimumLength = 6)
        => new(
            $"La contraseña debe contener al menos {minimumLength} caracteres",
            nameof(PasswordTooShort),
            new ErrorMessageProperty[]
            {
                new(nameof(minimumLength), minimumLength.ToString())
            }
        );

    public static ErrorMessage PasswordRequiresLower()
        => new(
            $"La contraseña debe contener al menos un caracter en minúscula",
            nameof(PasswordRequiresLower),
            null
        );

    public static ErrorMessage PasswordRequiresNonAlphanumeric()
        => new(
            $"La contraseña debe contener al menos un caracter que no sea alfanumerico",
            nameof(PasswordRequiresNonAlphanumeric),
            null
        );

    public static ErrorMessage PasswordRequiresUpper()
        => new(
            $"La contraseña debe contener al menos un caracter en mayúscula",
            nameof(PasswordRequiresUpper),
            null
        );

    public static ErrorMessage ProfilePictureTooSmall(int size, int minSize)
        => new(
            $"La foto de perfil es demasiado pequeña: {size}, minimo: {minSize}",
            nameof(ProfilePictureTooSmall),
            new ErrorMessageProperty[]
            {
                new(nameof(size), size.ToString()),
                new(nameof(minSize), minSize.ToString())
            }
        );

    public static ErrorMessage ProfilePictureTooLarge(int size, int maxSize)
        => new(
            $"La foto de perfil es demasiado grande: {size}, máximo: {maxSize}",
            nameof(ProfilePictureTooLarge),
            new ErrorMessageProperty[]
            {
                new(nameof(size), size.ToString()),
                new(nameof(maxSize), maxSize.ToString())
            }
        );

    public static ErrorMessage ProfilePictureNotSquare()
        => new(
            $"La foto de perfil debe de tener tanto la altura como la anchura de la imagen al mismo tamaño (debe de ser un cuadrado)",
            nameof(ProfilePictureNotSquare),
            null
        );

    public static string EncodeErrorMessage(string key, params object[]? arguments)
        => $"{key}:{(arguments is not null ? string.Join(',', arguments.Select(EncodeArgument)) : null)}";

    private static string? EncodeArgument(object argument)
    {
        var reg = EncodeCleanupRegex();
        if (argument is string str)
            return (string?)$"\"{reg.Replace(str, "")}\"";
        else 
        {
            var strarg = argument.ToString();
            return strarg is not null ? reg.Replace(strarg, "") : null;
        }
    }

    //public static (string Key, object[]? Arguments) DecodeErrorMessage(string encoded)
    //{
    //    var split = encoded.Split(':');
    //    return (split[0], )
    //}

    public static ErrorMessage TryBindError(string key, string? description, params object[]? arguments)
    {
        var method = typeof(ErrorMessages).GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(x => x.Name.Equals(key));
        if (method is not null)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != arguments?.Length)
            {
                if (parameters.Length > (arguments?.Length ?? 0))
                {
                    var newargs = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                        if (i < (arguments?.Length ?? 0))
                            newargs[i] = arguments![i];
                        else if (parameters[i].HasDefaultValue)
                            newargs[i] = parameters[i].DefaultValue!;
                        else
                            return new ErrorMessage(description, key, null);

                    return (ErrorMessage)method.Invoke(null, newargs)!;
                }
            }

            return (ErrorMessage)method.Invoke(null, arguments)!;
        }

        return new ErrorMessage(description, key, null);
    }

    [GeneratedRegex(@"["":]")]
    private static partial Regex EncodeCleanupRegex();
}
