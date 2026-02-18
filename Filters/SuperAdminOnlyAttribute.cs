// Filters/SuperAdminOnlyAttribute.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TMSBilling.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SuperAdminOnlyAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;

            // Cek login
            if (!session.Keys.Contains("username"))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Hanya SuperAdmin
            if (session.GetString("is_superadmin") != "true")
            {
                var isAjax = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                          || context.HttpContext.Request.ContentType?.Contains("application/json") == true;

                if (isAjax)
                {
                    context.Result = new JsonResult(new
                    {
                        success = false,
                        message = "Access denied. SuperAdmin only."
                    })
                    {
                        StatusCode = 403
                    };
                }
                else
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                }
            }
        }
    }
}