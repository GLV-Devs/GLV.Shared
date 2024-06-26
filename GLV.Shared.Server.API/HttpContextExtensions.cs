﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace GLV.Shared.Server.API;

public static class HttpContextExtensions
{
    public static string GetRequestingClientIPAddress(this HttpContext context)
    {
        var ipAddress = context.GetServerVariable("HTTP_X_FORWARDED_FOR");

        if (string.IsNullOrEmpty(ipAddress) is false)
        {
            var firstCommaIndex = ipAddress.IndexOf(',');
            return firstCommaIndex == -1 ? ipAddress : ipAddress[..firstCommaIndex];
        }

        return context.GetServerVariable("REMOTE_ADDR") ?? throw new InvalidOperationException("IServerVariablesFeature feature is not supported");
    }

    public static void SetRetryAfterHeader(this HttpResponse response, int secondsToWait)
    {
        response.Headers.RetryAfter = new StringValues(secondsToWait.ToString());
    }

    public static void SetRetryAfterHeader(this HttpResponse response, DateTimeOffset dateToRetryAt)
    {
        response.Headers.RetryAfter = new StringValues(dateToRetryAt.ToUniversalTime().ToString("ddd, dd MMM yyyy hh:mm:ss \\U\\T\\C"));
    }
}
