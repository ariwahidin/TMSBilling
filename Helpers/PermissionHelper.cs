// Helpers/PermissionHelper.cs
using TMSBilling.Services;

namespace TMSBilling.Helpers
{
    public static class PermissionHelper
    {
        public static bool HasPermission(this IHttpContextAccessor accessor, string code)
        {
            var session = accessor.HttpContext?.Session;
            if (session == null) return false;

            if (session.GetString("is_superadmin") == "true") return true;

            var stored = session.GetString("permissions") ?? "";
            return stored.Split(',').Contains(code);
        }
    }
}