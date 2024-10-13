using System.Collections;
using GLV.Shared.Data;
using GLV.Shared.DataTransfer;
using GLV.Shared.Server.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GLV.Shared.Server.API;

public static class APIServerResponseExtensions
{
    public static object? CreateServerResponse(this object? data, string traceIdentifier)
        => data switch
        {
            null => null,
            AsyncResultData resultData => AsyncResultDataExtensions.CreateServerResponse(resultData, traceIdentifier),
            ProblemDetails problem => ProblemDetailsExtensions.CreateServerResponse(problem, traceIdentifier),
            ErrorList errorList => ServerResponse.CreateServerResponse(errorList, traceIdentifier),
            IEnumerable<ErrorMessage> errors => ServerResponse.CreateServerResponse(errors, traceIdentifier),
            string str => ServerResponse.CreateServerResponseFromString(str, traceIdentifier),
            IEnumerable<string> strings => ServerResponse.CreateServerResponseFromString(strings, traceIdentifier),
            IEnumerable models => ServerResponse.CreateServerResponse(models, traceIdentifier),
            object model => ServerResponse.CreateServerResponseFromObject(model, traceIdentifier),
        };
}
