using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test Back function with Inertia request returns redirect status with location header.")]
    public async Task TestBackWithInertiaRequest()
    {
        var backResult = _factory.Back("/fallback");

        var headers = new HeaderDictionary
        {
            { "X-Inertia", "true" }
        };

        var responseHeaders = new HeaderDictionary();
        var response = new Mock<HttpResponse>();
        response.SetupGet(r => r.Headers).Returns(responseHeaders);
        response.SetupProperty(r => r.StatusCode);

        var request = new Mock<HttpRequest>();
        request.SetupGet(r => r.Headers).Returns(headers);

        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(c => c.Request).Returns(request.Object);
        httpContext.SetupGet(c => c.Response).Returns(response.Object);

        var context = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());

        await backResult.ExecuteResultAsync(context);

        // Should set status code to 303 (SeeOther) and location header to fallback URL
        Assert.That(response.Object.StatusCode, Is.EqualTo(303));
        Assert.That(responseHeaders["Location"].ToString(), Is.EqualTo("/fallback"));
    }

    [Test]
    [Description("Test Back function with regular request and referrer header redirects to referrer.")]
    public async Task TestBackWithReferrerHeader()
    {
        var backResult = _factory.Back("/fallback");

        var headers = new HeaderDictionary
        {
            { "Referer", "https://example.com/previous-page" }
        };

        var responseHeaders = new HeaderDictionary();
        var response = new Mock<HttpResponse>();
        response.SetupGet(r => r.Headers).Returns(responseHeaders);
        response.SetupProperty(r => r.StatusCode);

        var request = new Mock<HttpRequest>();
        request.SetupGet(r => r.Headers).Returns(headers);

        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(c => c.Request).Returns(request.Object);
        httpContext.SetupGet(c => c.Response).Returns(response.Object);

        var context = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());

        var result = backResult as IActionResult;
        Assert.That(result, Is.Not.Null);

        await result.ExecuteResultAsync(context);

        // Should set status code to 303 (SeeOther) and location header to referrer
        Assert.That(response.Object.StatusCode, Is.EqualTo(303));
        Assert.That(responseHeaders["Location"].ToString(), Is.EqualTo("https://example.com/previous-page"));
    }

    [Test]
    [Description("Test Back function without referrer uses fallback URL.")]
    public async Task TestBackWithFallbackUrl()
    {
        var backResult = _factory.Back("/custom-fallback");

        var headers = new HeaderDictionary();

        var responseHeaders = new HeaderDictionary();
        var response = new Mock<HttpResponse>();
        response.SetupGet(r => r.Headers).Returns(responseHeaders);
        response.SetupProperty(r => r.StatusCode);

        var request = new Mock<HttpRequest>();
        request.SetupGet(r => r.Headers).Returns(headers);

        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(c => c.Request).Returns(request.Object);
        httpContext.SetupGet(c => c.Response).Returns(response.Object);

        var context = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());

        var result = backResult as IActionResult;
        Assert.That(result, Is.Not.Null);

        await result.ExecuteResultAsync(context);

        // Should set status code to 303 (SeeOther) and location header to custom fallback
        Assert.That(response.Object.StatusCode, Is.EqualTo(303));
        Assert.That(responseHeaders["Location"].ToString(), Is.EqualTo("/custom-fallback"));
    }

    [Test]
    [Description("Test Back function without fallback URL uses default root path.")]
    public async Task TestBackWithDefaultFallback()
    {
        var backResult = _factory.Back();

        var headers = new HeaderDictionary();

        var responseHeaders = new HeaderDictionary();
        var response = new Mock<HttpResponse>();
        response.SetupGet(r => r.Headers).Returns(responseHeaders);
        response.SetupProperty(r => r.StatusCode);

        var request = new Mock<HttpRequest>();
        request.SetupGet(r => r.Headers).Returns(headers);

        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(c => c.Request).Returns(request.Object);
        httpContext.SetupGet(c => c.Response).Returns(response.Object);

        var context = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());

        var result = backResult as IActionResult;
        Assert.That(result, Is.Not.Null);

        await result.ExecuteResultAsync(context);

        // Should set status code to 303 (SeeOther) and location header to default fallback
        Assert.That(response.Object.StatusCode, Is.EqualTo(303));
        Assert.That(responseHeaders["Location"].ToString(), Is.EqualTo("/"));
    }

    [Test]
    [Description("Test Back function with permanent redirect.")]
    public async Task TestBackWithPermanentRedirect()
    {
        var backResult = _factory.Back("/fallback", HttpStatusCode.MovedPermanently);

        var headers = new HeaderDictionary();

        var responseHeaders = new HeaderDictionary();
        var response = new Mock<HttpResponse>();
        response.SetupGet(r => r.Headers).Returns(responseHeaders);
        response.SetupProperty(r => r.StatusCode);

        var request = new Mock<HttpRequest>();
        request.SetupGet(r => r.Headers).Returns(headers);

        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(c => c.Request).Returns(request.Object);
        httpContext.SetupGet(c => c.Response).Returns(response.Object);

        var context = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());

        var result = backResult as IActionResult;
        Assert.That(result, Is.Not.Null);

        await result.ExecuteResultAsync(context);

        // Should set status code to 301 (MovedPermanently) and location header to fallback
        Assert.That(response.Object.StatusCode, Is.EqualTo(301));
        Assert.That(responseHeaders["Location"].ToString(), Is.EqualTo("/fallback"));
    }
}
