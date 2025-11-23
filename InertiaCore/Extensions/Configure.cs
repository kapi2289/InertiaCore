using InertiaCore.Models;
using InertiaCore.Ssr;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace InertiaCore.Extensions;

public static class Configure
{
    public static IApplicationBuilder UseInertia(this IApplicationBuilder app)
    {
        var factory = app.ApplicationServices.GetRequiredService<IResponseFactory>();
        Inertia.UseFactory(factory);

        var viteBuilder = app.ApplicationServices.GetService<IViteBuilder>();
        if (viteBuilder != null)
        {
            Vite.UseBuilder(viteBuilder);
            Inertia.Version(Vite.GetManifestHash);
        }

        // Check if TempData services are available for error bag functionality
        CheckTempDataAvailability(app);

        app.UseMiddleware<Middleware>();

        return app;
    }

    private static void CheckTempDataAvailability(IApplicationBuilder app)
    {
        // Skip warning in test environments
        var environment = app.ApplicationServices.GetService<IWebHostEnvironment>();
        if (environment?.EnvironmentName == "Test" ||
            (environment?.EnvironmentName != "Development" && IsTestEnvironment()))
        {
            return;
        }

        try
        {
            var tempDataFactory = app.ApplicationServices.GetService<ITempDataDictionaryFactory>();
            if (tempDataFactory == null)
            {
                var logger = app.ApplicationServices.GetService<ILogger<IApplicationBuilder>>();
                logger?.LogWarning("TempData services are not configured. Error bag functionality will be limited. " +
                                   "Consider adding services.AddSession() and app.UseSession() to enable full error bag support.");
            }
        }
        catch (Exception)
        {
            // If we can't check for TempData services, that's also a sign they might not be configured
            var logger = app.ApplicationServices.GetService<ILogger<IApplicationBuilder>>();
            logger?.LogWarning("Unable to verify TempData configuration. Error bag functionality may be limited. " +
                               "Ensure services.AddSession() and app.UseSession() are configured for full error bag support.");
        }
    }

    private static bool IsTestEnvironment()
    {
        // Check if we're running in a test context by looking for common test assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        return assemblies.Any(a =>
            a.FullName?.Contains("nunit", StringComparison.OrdinalIgnoreCase) == true ||
            a.FullName?.Contains("xunit", StringComparison.OrdinalIgnoreCase) == true ||
            a.FullName?.Contains("mstest", StringComparison.OrdinalIgnoreCase) == true ||
            a.FullName?.Contains("testhost", StringComparison.OrdinalIgnoreCase) == true);
    }

    public static IServiceCollection AddInertia(this IServiceCollection services,
        Action<InertiaOptions>? options = null)
    {
        services.AddHttpContextAccessor();
        services.AddHttpClient();

        services.AddSingleton<IResponseFactory, ResponseFactory>();
        services.AddSingleton<IGateway, Gateway>();
        services.AddSingleton<IInertiaSerializer, DefaultInertiaSerializer>();

        services.Configure<MvcOptions>(mvcOptions => { mvcOptions.Filters.Add<InertiaActionFilter>(); });

        if (options != null) services.Configure(options);

        return services;
    }

    public static IServiceCollection UseInertiaSerializer<TImplementation>(this IServiceCollection services)
        where TImplementation : IInertiaSerializer
    {
        services.Replace(
            new ServiceDescriptor(typeof(IInertiaSerializer), typeof(TImplementation), ServiceLifetime.Singleton)
        );

        return services;
    }

    public static IServiceCollection AddViteHelper(this IServiceCollection services,
        Action<ViteOptions>? options = null)
    {
        services.AddSingleton<IViteBuilder, ViteBuilder>();
        if (options != null) services.Configure(options);

        return services;
    }
}
