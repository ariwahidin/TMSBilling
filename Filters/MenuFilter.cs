// Filters/MenuFilter.cs
using Microsoft.AspNetCore.Mvc.Filters;
using TMSBilling.Services;

namespace TMSBilling.Filters
{
    public class MenuFilter : IAsyncActionFilter
    {
        private readonly IPermissionService _permissionService;

        public MenuFilter(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Hanya inject menu jika user sudah login
            if (context.HttpContext.Session.Keys.Contains("username"))
            {
                var controller = context.Controller as Microsoft.AspNetCore.Mvc.Controller;
                if (controller != null)
                {
                    controller.ViewBag.Menus = await _permissionService.GetAccessibleMenusAsync();
                }
            }

            await next();
        }
    }
}