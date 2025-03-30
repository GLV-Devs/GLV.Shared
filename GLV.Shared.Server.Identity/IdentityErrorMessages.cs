using GLV.Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Server.Identity;

public static class IdentityErrorMessages
{
    public static ErrorMessage ModifiedUserLevelTooHigh()
        => new(
            "The user level of the user being modified is too high",
            nameof(ModifiedUserLevelTooHigh),
            null
        );

    public static ErrorMessage CurrentUserLevelTooLow()
        => new(
            "The user level of the current user is too low",
            nameof(CurrentUserLevelTooLow),
            null
        );

    public static ErrorMessage InvalidUserPermissions()
        => new(
            "The current user does not have the required permissions to adjust the requested permissions",
            nameof(InvalidUserPermissions),
            null
        );
}
