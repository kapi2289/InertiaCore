using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InertiaCore.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;

namespace InertiaCore.Ssr;

internal interface IGateway
{
    public Task<SsrResponse?> Dispatch(object model, string url);
    public bool ShouldDispatch();
}

internal class Gateway : IGateway
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<InertiaOptions> _options;
    private readonly IWebHostEnvironment _environment;

    public Gateway(IHttpClientFactory httpClientFactory, IOptions<InertiaOptions> options, IWebHostEnvironment environment) =>
        (_httpClientFactory, _options, _environment) = (httpClientFactory, options, environment);

    public async Task<SsrResponse?> Dispatch(dynamic model, string url)
    {
        var json = JsonSerializer.Serialize(model,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            });
        var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync(url, content);
        return await response.Content.ReadFromJsonAsync<SsrResponse>();
    }

    public bool ShouldDispatch()
    {
        return !_options.Value.SsrEnsureBundleExists || BundleExists();
    }

    private bool BundleExists()
    {
        var commonBundlePaths = new[]
        {
            "~/public/js/ssr.js",
            "~/public/build/ssr.js",
            "~/wwwroot/js/ssr.js",
            "~/wwwroot/build/ssr.js",
            "~/dist/ssr.js",
            "~/build/ssr.js"
        };

        foreach (var path in commonBundlePaths)
        {
            var resolvedPath = ResolvePath(path);
            if (!string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
            {
                return true;
            }
        }

        return false;
    }

    private string? ResolvePath(string path)
    {
        if (path.StartsWith("~/"))
        {
            return Path.Combine(_environment.ContentRootPath, path[2..]);
        }
        return Path.IsPathRooted(path) ? path : Path.Combine(_environment.ContentRootPath, path);
    }
}
