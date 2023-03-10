using InertiaCore;
using InertiaCore.Extensions;
using InertiaCore.Ssr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moq;

namespace InertiaCoreTests;

public partial class Tests
{
    private IResponseFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var httpClientFactory = new Mock<IHttpClientFactory>();

        var gateway = new Gateway(httpClientFactory.Object);
        var options = new Mock<IOptions<InertiaOptions>>();
        options.SetupGet(x => x.Value).Returns(new InertiaOptions());

        _factory = new ResponseFactory(contextAccessor.Object, gateway, options.Object);
    }

    /// <summary>
    /// Prepares ActionContext for usage in tests.
    /// </summary>
    /// <param name="headers">Optional request headers.</param>
    /// <param name="sharedData">Optional Inertia shared data.</param>
    /// <param name="modelState">Optional validation errors dictionary.</param>
    private static ActionContext PrepareContext(HeaderDictionary? headers = null, InertiaSharedData? sharedData = null,
        Dictionary<string, string>? modelState = null)
    {
        var request = new Mock<HttpRequest>();
        request.SetupGet(r => r.Headers).Returns(headers ?? new HeaderDictionary());

        var response = new Mock<HttpResponse>();
        response.SetupGet(r => r.Headers).Returns(new HeaderDictionary());

        var features = new FeatureCollection();
        if (sharedData != null)
            features.Set(sharedData);

        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(c => c.Request).Returns(request.Object);
        httpContext.SetupGet(c => c.Response).Returns(response.Object);
        httpContext.SetupGet(c => c.Features).Returns(features);

        var context = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());

        if (modelState == null) return context;

        foreach (var (key, value) in modelState)
        {
            context.ModelState.AddModelError(key, value);
        }

        return context;
    }
}
