using GLV.Shared.Data;
using Microsoft.AspNetCore.Identity;
using System.Net;
using GLV.Shared.Server.Data;

namespace GLV.Shared.Server.API;

public static class ErrorExtensions
{
    public static ref ErrorList AddIdentityErrors(this ref ErrorList errors, IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            var msg = ErrorMessages.TryBindError(error.Code, error.Description);
            errors.AddError(msg);
        }

        return ref errors;
    }
}
