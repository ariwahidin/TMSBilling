using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TMSBilling.Filters // pakai namespace sesuai folder
{
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            if (!httpContext.Session.Keys.Contains("username"))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }
    }
}