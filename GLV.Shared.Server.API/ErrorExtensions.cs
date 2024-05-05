using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLV.Shared.Data;
using Microsoft.AspNetCore.Identity;

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
