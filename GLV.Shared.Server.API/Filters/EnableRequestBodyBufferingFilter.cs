using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GLV.Shared.Server.API.Filters;

public class EnableRequestBodyBufferingAttribute : TypeFilterAttribute
{
    public EnableRequestBodyBufferingAttribute() : base(typeof(EnableRequestBodyBufferingFilter))
    {
    }
}

public class EnableRequestBodyBufferingFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        context.HttpContext.Request.EnableBuffering();
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
