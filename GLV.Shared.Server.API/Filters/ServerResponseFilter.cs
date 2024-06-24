using System.Collections;
using System.Diagnostics;
using GLV.Shared.Data;
using GLV.Shared.DataTransfer;
using GLV.Shared.Server.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GLV.Shared.Server.API.Filters;

public class ServerResponseFilter : IResultFilter
{
    private sealed class ServerAsyncResponse<T>(string? dataType, string traceId, IAsyncEnumerable<T>? data)
    {
        public string? DataType => dataType;

        public string TraceId { get; set; } = traceId ?? throw new ArgumentNullException(nameof(traceId));

        public IAsyncEnumerable<T>? Data { get; set; } = data;
    }

    [ThreadStatic]
    private static Type[]? ServerAsyncResponseGenericTypeBuffer;

    [ThreadStatic]
    private static object[]? ServerAsyncResponseConstructorParametersBuffer;

    private object BuildServerAsyncResponse(AsyncResultData data, string traceId)
    {
        var typeArgs = ServerAsyncResponseGenericTypeBuffer ??= [null!];
        typeArgs[0] = data.AsyncEnumerableType;

        var ctorParams = ServerAsyncResponseConstructorParametersBuffer ?? [null!, null!, null!];
        ctorParams[0] = data.AsyncEnumerableType.Name;
        ctorParams[1] = traceId;
        ctorParams[2] = data.Data!;

        var inst = Activator.CreateInstance(typeof(ServerAsyncResponse<>).MakeGenericType(typeArgs), ctorParams)!;
        Array.Clear(ctorParams);
        return inst;
    }

    protected ActionResult? FillAPIResponseObject(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult objresult)
        {
            switch (objresult.Value)
            {
                case AsyncResultData resultData:
                    objresult.Value = BuildServerAsyncResponse(resultData, context.HttpContext.TraceIdentifier);
                    break;

                case ProblemDetails problem:
                    var pdlist = new ErrorList();
                    pdlist.AddError(new ErrorMessage($"{problem.Title}: {problem.Detail}", "Unknown", null));
                    objresult.Value = new ServerResponse(nameof(ErrorList), context.HttpContext.TraceIdentifier, pdlist.Errors);
                    break;

                case ErrorList errorList:
                    objresult.Value = new ServerResponse(nameof(ErrorList), context.HttpContext.TraceIdentifier, errorList.Errors);
                    break;

                case IEnumerable<ErrorMessage> errors:
                    objresult.Value = new ServerResponse(nameof(ErrorList), context.HttpContext.TraceIdentifier, errors);
                    break;

                case string:
                case IEnumerable<string>:
                    objresult.Value = new ServerResponse(
                        typeof(string).Name,
                        context.HttpContext.TraceIdentifier,
                        objresult.Value is string str
                            ? [str]
                            : (IEnumerable<string>)objresult.Value
                    );
                    break;

                case IEnumerable models:
                    var first = models.Cast<object>().FirstOrDefault();
                    objresult.Value = first is null
                        ? new ServerResponse(null, context.HttpContext.TraceIdentifier, null)
                        : new ServerResponse(first.GetType().Name, context.HttpContext.TraceIdentifier, models);
                    break;

                case object model:
                    objresult.Value = new ServerResponse(model.GetType().Name, context.HttpContext.TraceIdentifier, new[] { model });
                    break;

                case null:
                    objresult.Value = null;
                    break;
            }

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
