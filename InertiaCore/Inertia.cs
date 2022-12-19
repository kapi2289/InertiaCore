using Microsoft.AspNetCore.Html;

namespace InertiaCore;

public static class Inertia
{
    private static IResponseFactory _factory = default!;

    internal static void UseFactory(IResponseFactory factory) => _factory = factory;

    public static Response Render(string component, object? props = null) => _factory.Render(component, props);

    public static IHtmlContent Html(dynamic model) => _factory.Html(model);

    public static void SetRootView(string rootView) => _factory.SetRootView(rootView);

    public static void Version(object? version) => _factory.Version(version);

    public static string? GetVersion() => _factory.GetVersion();

    public static LocationResult Location(string url) => _factory.Location(url);

    public static void Share(string key, object? value) => _factory.Share(key, value);

    public static void Share(IDictionary<string, object?> data) => _factory.Share(data);
}
