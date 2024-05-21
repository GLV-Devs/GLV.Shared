using GLV.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace GLV.Shared.EntityFramework;

public static class DbContextExtensions
{
    public static async ValueTask<SuccessResult> TrySaveChanges(this DbContext context)
    {
        try
        {
            await context.SaveChangesAsync();
            return SuccessResult.Success;
        }
        catch (DbUpdateException e)
        {
            ErrorList errors = new();
            var msg = e.InnerException!.Message;
            if (msg.Contains("foreign key", StringComparison.OrdinalIgnoreCase))
            {
                var match = DataRegexes.DatabaseExceptionMessageForeignKey().Match(msg);
                if (match.Success)
                {
                    if (match.Groups.TryGetValue("entity", out var group))
                    {
                        errors.AddEntityNotFound(group.Value, null);
                        return errors;
                    }
                }
            }

            throw;
        }
    }
}
