using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Samples.Blazor.Server.Controllers
{
    public class BlazorModeController : ControllerBase
    {
        public static string CookieName { get; set; } = "_ssb_";

        [Route("_blazorMode/{isServerSideBlazor}")]
        public IActionResult Mode(bool isServerSideBlazor, string? redirectTo = null)
        {
            if (isServerSideBlazor != IsServerSideBlazor(HttpContext)) {
                var response = HttpContext.Response;
                response.Cookies.Append(CookieName, Convert.ToInt32(isServerSideBlazor).ToString());
            }
            if (string.IsNullOrEmpty(redirectTo))
                redirectTo = "~/";
            return Redirect(redirectTo);
        }

        public static bool IsServerSideBlazor(HttpContext httpContext)
        {
            var cookies = httpContext.Request.Cookies;
            var ssb = cookies.TryGetValue(CookieName, out var v1) ? v1 : "";
            return int.TryParse(ssb, out var v2) && v2 != 0;
        }
    }
}