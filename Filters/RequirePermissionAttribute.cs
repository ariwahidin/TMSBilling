// Filters/RequirePermissionAttribute.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TMSBilling.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _permissionCode;

        public RequirePermissionAttribute(string permissionCode)
        {
            _permissionCode = permissionCode;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;

            // 1. Cek login dulu
            if (!session.Keys.Contains("username"))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // 2. SuperAdmin bypass semua permission check
            if (session.GetString("is_superadmin") == "true")
                return;

            // 3. Cek permission dari session
            var stored = session.GetString("permissions") ?? "";
            var permissions = stored.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (!permissions.Contains(_permissionCode))
            {
                // Bedakan request AJAX vs regular request
                var isAjax = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                          || context.HttpContext.Request.ContentType?.Contains("application/json") == true;

                if (isAjax)
                {
                    context.Result = new JsonResult(new
                    {
                        success = false,
                        message = "Access denied. You don't have permission to perform this action."
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