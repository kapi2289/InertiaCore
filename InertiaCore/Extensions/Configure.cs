using System.Net;
using InertiaCore.Ssr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Extensions;

public static class Configure
{
    public static IApplicationBuilder UseInertia(this IApplicationBuilder app)
    {
        var factory = app.ApplicationServices.GetRequiredService<IResponseFactory>();
        Inertia.UseFactory(factory);

        app.Use(async (context, next) =>
        {
            if (context.IsInertiaRequest()
                && context.Request.Method == "GET"
                && context.Request.Headers["X-Inertia-Version"] != Inertia.GetVersion())
            {
                await OnVersionChange(context, app);
                return;
            }

            await next();
        });

        return app;
    }

    public static IServiceCollection AddInertia(this IServiceCollection services,
        Action<InertiaOptions>? options = null)
    {
        services.AddHttpContextAccessor();
        services.AddHttpClient();

        services.AddSingleton<IResponseFactory, ResponseFactory>();
        services.AddSingleton<IGateway, Gateway>();

        services.Configure<MvcOptions>(mvcOptions => { mvcOptions.Filters.Add<InertiaActionFilter>(); });

        if (options != null) services.Configure(options);

        return services;
    }

    private static async Task OnVersionChange(HttpContext context, IApplicationBuilder app)
    {
        var tempData = app.ApplicationServices.GetRequiredService<TempDataDictionaryFactory>()
            .GetTempData(context);

        if (tempData.Any()) tempData.Keep();

        context.Response.Headers.Add("X-Inertia-Location", context.RequestedUri());
        context.Response.StatusCode = (int)HttpStatusCode.Conflict;

        await context.Response.CompleteAsync();
    }
}
