using System.Net;
using InertiaCore.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;

namespace InertiaCore.Utils;

internal class InertiaActionFilter : IActionFilter
{
    private readonly IUrlHelperFactory _urlHelperFactory;

    public InertiaActionFilter(IUrlHelperFactory urlHelperFactory) => _urlHelperFactory = urlHelperFactory;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        //
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (!context.IsInertiaRequest()
            || !new[] { "PUT", "PATCH", "DELETE" }.Contains(context.HttpContext.Request.Method)) return;

        var destinationUrl = context.Result switch
        {
            RedirectResult result => result.Url,
            RedirectToActionResult result => GetUrl(result, context),
            RedirectToPageResult result => GetUrl(result, context),
            RedirectToRouteResult result => GetUrl(result, context),
            _ => null
        };

        if (destinationUrl == null) return;
        context.HttpContext.Response.Headers.Add("Location", destinationUrl);
        context.Result = new StatusCodeResult((int)HttpStatusCode.RedirectMethod);
    }

    private string? GetUrl(RedirectToActionResult result, ActionContext context)
    {
        var urlHelper = result.UrlHelper ?? _urlHelperFactory.GetUrlHelper(context);

        return urlHelper.Action(
            result.ActionName,
            result.ControllerName,
            result.RouteValues,
            null,
            null,
            result.Fragment);
    }

    private string? GetUrl(RedirectToPageResult result, ActionContext context)
    {
        var urlHelper = result.UrlHelper ?? _urlHelperFactory.GetUrlHelper(context);

        return urlHelper.Page(
            result.PageName,
            result.PageHandler,
            result.RouteValues,
            result.Protocol,
            result.Host,
            result.Fragment);
    }

    private string? GetUrl(RedirectToRouteResult result, ActionContext context)
    {
        var urlHelper = result.UrlHelper ?? _urlHelperFactory.GetUrlHelper(context);

        return urlHelper.RouteUrl(
            result.RouteName,
            result.RouteValues,
            null,
            null,
            result.Fragment);
    }
}
