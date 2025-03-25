using System.Diagnostics;
using GLV.Shared.Data;
using GLV.Shared.DataTransfer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using GLV.Shared.Server.Data;

namespace GLV.Shared.Server.API.Filters;

public class ServerResponseFilter : IResultFilter
{
    protected ActionResult? FillAPIResponseObject(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult objresult)
        {
            objresult.Value = objresult.Value.CreateServerResponse(context.HttpContext.TraceIdentifier);

            return objresult;
        }
        else if (context.Result is StatusCodeResult statusResult)
        {
            if (statusResult.StatusCode is int sc && sc >= 200 && sc <= 299)
                return new ObjectResult(new ServerResponse(null, context.HttpContext.TraceIdentifier, null))
                {
                    StatusCode = statusResult.StatusCode
                };

            return new ObjectResult(new ServerResponse(nameof(ErrorList), context.HttpContext.TraceIdentifier, new ErrorMessage[]
            {
                ErrorMessages.Unspecified()
            }))
            {
                StatusCode = statusResult.StatusCode
            };
        }
        else if (context.Result is null or EmptyResult or FileStreamResult or FileContentResult)
            return null;
        else
        {
            Debugger.Break();
            throw new InvalidDataException($"The result of the request was unexpected. Found: {context.Result?.GetType().Name}");
        }
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        var x = FillAPIResponseObject(context);
        if (x is not null)
            context.Result = x;
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {

    }
}
