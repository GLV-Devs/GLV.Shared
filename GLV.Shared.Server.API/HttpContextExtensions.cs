using GLV.Shared.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Net;

namespace GLV.Shared.Server.API;

public static class HttpContextExtensions
{
    public static void SetResponseStatusCode(this HttpContext context, ref ErrorList errors)
    {
        if (errors.RecommendedCode is HttpStatusCode code)
            context.Response.StatusCode = (int)code;
    }

    public static string GetRequestingClientIPAddress(this HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(ipAddress) is false)
            return getIp(ipAddress); 
        
        ipAddress = context.GetServerVariable("HTTP_X_FORWARDED_FOR");

        if (string.IsNullOrEmpty(ipAddress) is false)
            return getIp(ipAddress);

        return context.GetServerVariable("REMOTE_ADDR") ?? throw new InvalidOperationException("IServerVariablesFeature feature is not supported");

        string getIp(string ip)
        {
            var firstCommaIndex = ipAddress.IndexOf(',');
            return firstCommaIndex == -1 ? ipAddress : ipAddress[..firstCommaIndex];
        }
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
