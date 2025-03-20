using GLV.Shared.Data;
using GLV.Shared.DataTransfer;
using Microsoft.AspNetCore.Mvc;
namespace GLV.Shared.Server.API.Controllers;

public static class ProblemDetailsExtensions
{
    public static IServerResponse CreateServerResponse(this ProblemDetails problem, string traceIdentifier)
    {
        var pdlist = new ErrorList();
        pdlist.AddError(new ErrorMessage($"{problem.Title}: {problem.Detail}", "Unknown", null));
        return new ServerResponse(nameof(ErrorList), traceIdentifier, pdlist.Errors);
    }
}
