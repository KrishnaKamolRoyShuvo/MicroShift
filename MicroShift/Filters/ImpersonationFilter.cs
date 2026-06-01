using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

public class ImpersonationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        if (session.GetString("IsImpersonating") == "true")
        {
            // Inject the impersonated user ID into the request context
            // You can access this in your controllers via context.HttpContext.User
        }
    }
    public void OnActionExecuted(ActionExecutedContext context) { }
}