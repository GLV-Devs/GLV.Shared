using System.Collections;
using System.Diagnostics;
using System.Net;
using GLV.Shared.Data;
using GLV.Shared.DataTransfer;
using GLV.Shared.Server.Data;
using GLV.Shared.Server.API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace GLV.Shared.Server.API;

public static class APIServerResponseExtensions
{
    public static IActionResult ForbiddenResult(this ControllerBase controller, DisallowableAction action, DisallowableActionTarget target = DisallowableActionTarget.Entity, string? targetName = null)
    {
        var errors = new ErrorList();
        errors.AddActionDisallowed(action, target, targetName);
        return controller.CreateServerResponseResult(errors);
    }

    public static IActionResult EntityNotFoundResult(this ControllerBase controller, string entityName, string? query = null)
    {
        var errors = new ErrorList();
        errors.AddEntityNotFound(entityName, query);
        return controller.CreateServerResponseResult(errors);
    }

    public static ObjectResult CreateServerResponseResult<T>(this ControllerBase controller, SuccessResult<T> result)
    {
        IServerResponse? resp; 

        if (result.IsSuccess is false)
        {
            resp = ServerResponse.CreateServerResponse(result.ErrorMessages, controller.HttpContext.TraceIdentifier);
            return new ObjectResult(resp) { StatusCode = (int)(result.ErrorMessages.RecommendedCode ?? HttpStatusCode.BadRequest) };
        }

        resp = result.Result.CreateServerResponse(controller.HttpContext.TraceIdentifier);
        return resp is null ? new ObjectResult(new ServerResponse(null, controller.HttpContext.TraceIdentifier, null)) { StatusCode = 204 } : new ObjectResult(resp) { StatusCode = 200 };
    }

    public static ObjectResult CreateServerResponseResult(this ControllerBase controller, SuccessResult result)
    {
        if (result.IsSuccess)
            return new ObjectResult(new ServerResponse(null, controller.HttpContext.TraceIdentifier, null)) { StatusCode = 204 };

        var resp = ServerResponse.CreateServerResponse(result.ErrorMessages, controller.HttpContext.TraceIdentifier);
        return new ObjectResult(resp) { StatusCode = (int)(result.ErrorMessages.RecommendedCode ?? HttpStatusCode.BadRequest) };
    }

    public static ObjectResult CreateServerErrorResult(this ControllerBase controller, HttpStatusCode code, params Span<ErrorMessage> messages)
    {
        var err = new ErrorList(code);
        foreach (var e in messages) 
            err.AddError(e);

        var resp = ServerResponse.CreateServerResponse(err, controller.HttpContext.TraceIdentifier);

        return new ObjectResult(resp) { StatusCode = (int)(err.RecommendedCode ?? HttpStatusCode.BadRequest) };
    }

    public static ObjectResult CreateServerResponseResult<TData>(this ControllerBase controller, TData? data)
    {
        IServerResponse resp = data.CreateServerResponse(controller.HttpContext.TraceIdentifier);

        if (data is ErrorList errorList)
            return new ObjectResult(resp) { StatusCode = (int)(errorList.RecommendedCode ?? HttpStatusCode.BadRequest) };

        if (data is IEnumerable<ErrorMessage>)
            return new ObjectResult(resp) { StatusCode = (int)HttpStatusCode.BadRequest };

        return new ObjectResult(resp) { StatusCode = 200 };
    }

    public static ObjectResult CreateServerResponseResult(this ControllerBase controller, object? data)
    {
        IServerResponse? resp = data.CreateServerResponse(controller.HttpContext.TraceIdentifier);
        if (resp is null)
            return new ObjectResult(new ServerResponse(null, controller.HttpContext.TraceIdentifier, null)) { StatusCode = 204 };

        if (data is ErrorList errorList)
            return new ObjectResult(resp) { StatusCode = (int)(errorList.RecommendedCode ?? HttpStatusCode.BadRequest) };

        if (data is IEnumerable<ErrorMessage>)
            return new ObjectResult(resp) { StatusCode = (int)HttpStatusCode.BadRequest };

        return new ObjectResult(resp) { StatusCode = 200 };
    }

    public static IServerResponse CreateServerResponse(this object? data, string traceIdentifier)
        => data switch
        {
            null => new ServerResponse(null, traceIdentifier, null),
            AsyncResultData resultData => AsyncResultDataExtensions.CreateServerResponse(resultData, traceIdentifier),
            ProblemDetails problem => ProblemDetailsExtensions.CreateServerResponse(problem, traceIdentifier),
            ErrorList errorList => ServerResponse.CreateServerResponse(errorList, traceIdentifier),
            IEnumerable<ErrorMessage> errors => ServerResponse.CreateServerResponse(errors, traceIdentifier),
            string str => ServerResponse.CreateServerResponseFromString(str, traceIdentifier),
            IEnumerable<string> strings => ServerResponse.CreateServerResponseFromString(strings, traceIdentifier),
            IEnumerable models => ServerResponse.CreateServerResponse(models, traceIdentifier),
            object model => ServerResponse.CreateServerResponseFromObject(model, traceIdentifier),
        };

    public static IServerResponse CreateServerResponse<TData>(this TData? data, string traceIdentifier)
    {
        if (data is null)
        {
            return typeof(TData).IsAssignableTo(typeof(IEnumerable))
                ? new ServerResponse(typeof(TData).GetCollectionInnerType().Name, traceIdentifier, null)
                : new ServerResponse(typeof(TData).Name, traceIdentifier, null);
        }

        Debug.Assert(data is not null);
        return data switch
        {
            AsyncResultData resultData => AsyncResultDataExtensions.CreateServerResponse(resultData, traceIdentifier),
            ProblemDetails problem => ProblemDetailsExtensions.CreateServerResponse(problem, traceIdentifier),
            ErrorList errorList => ServerResponse.CreateServerResponse(errorList, traceIdentifier),
            IEnumerable<ErrorMessage> errors => ServerResponse.CreateServerResponse(errors, traceIdentifier),
            string str => ServerResponse.CreateServerResponseFromString(str, traceIdentifier),
            IEnumerable<string> strings => ServerResponse.CreateServerResponseFromString(strings, traceIdentifier),
            IEnumerable models => ServerResponse.CreateServerResponse<TData>(models, traceIdentifier),
            object model => ServerResponse.CreateServerResponseFromObject(model, traceIdentifier),
        };
    }
}
