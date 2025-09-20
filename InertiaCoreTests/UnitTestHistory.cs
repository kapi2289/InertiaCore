using InertiaCore;
using InertiaCore.Models;
using InertiaCore.Ssr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if history encryption is sent correctly.")]
    public async Task TestHistoryEncryptionResult()
    {
        _factory.EncryptHistory();

        var response = _factory.Render("Test/Page", new
        {
            Test = "Test"
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia", "true" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var result = response.GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<JsonResult>());

            var json = (result as JsonResult)?.Value;
            Assert.That(json, Is.InstanceOf<Page>());

            Assert.That((json as Page)?.ClearHistory, Is.EqualTo(false));
            Assert.That((json as Page)?.EncryptHistory, Is.EqualTo(true));
            Assert.That((json as Page)?.Component, Is.EqualTo("Test/Page"));
            Assert.That((json as Page)?.Props, Is.EqualTo(new Dictionary<string, object?>
            {
                { "test", "Test" },
                { "errors", new Dictionary<string, string>(0) }
            }));
        });
    }

    [Test]
    [Description("Test if clear history is sent correctly.")]
    public async Task TestClearHistoryResult()
    {
        // Set up session mock
        var sessionData = new Dictionary<string, byte[]>();
        var sessionMock = new Mock<ISession>();

        sessionMock.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((key, value) => sessionData[key] = value);

        sessionMock.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) => sessionData.TryGetValue(key, out value));

        sessionMock.Setup(s => s.Remove(It.IsAny<string>()))
            .Callback<string>(key => sessionData.Remove(key));

        // Set up HttpContext with session support
        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.SetupGet(c => c.Session).Returns(sessionMock.Object);

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        contextAccessorMock.SetupGet(a => a.HttpContext).Returns(httpContextMock.Object);

        // Create factory with session support
        var gateway = new Mock<IGateway>();
        var options = new Mock<IOptions<InertiaOptions>>();
        options.SetupGet(x => x.Value).Returns(new InertiaOptions());
        var factoryWithSession = new ResponseFactory(contextAccessorMock.Object, gateway.Object, options.Object);

        factoryWithSession.ClearHistory();

        var response = factoryWithSession.Render("Test/Page", new
        {
            Test = "Test"
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia", "true" }
        };

        var context = PrepareContextWithSession(headers, sessionMock.Object);

        response.SetContext(context);
        await response.ProcessResponse();

        var result = response.GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<JsonResult>());

            var json = (result as JsonResult)?.Value;
            Assert.That(json, Is.InstanceOf<Page>());

            Assert.That((json as Page)?.ClearHistory, Is.EqualTo(true));
            Assert.That((json as Page)?.EncryptHistory, Is.EqualTo(false));
            Assert.That((json as Page)?.Component, Is.EqualTo("Test/Page"));
            Assert.That((json as Page)?.Props, Is.EqualTo(new Dictionary<string, object?>
            {
                { "test", "Test" },
                { "errors", new Dictionary<string, string>(0) }
            }));
        });

        // Verify session value was removed after being read (one-time use behavior)
        sessionMock.Verify(s => s.Remove("inertia.clear_history"), Times.Once);
    }

    [Test]
    [Description("Test if clear history persists when redirecting.")]
    public async Task TestClearHistoryWithRedirect()
    {
        // Arrange: Set up session mock to simulate session storage behavior
        var sessionData = new Dictionary<string, byte[]>();
        var sessionMock = new Mock<ISession>();

        sessionMock.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((key, value) => sessionData[key] = value);

        sessionMock.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) => sessionData.TryGetValue(key, out value));

        sessionMock.Setup(s => s.Remove(It.IsAny<string>()))
            .Callback<string>(key => sessionData.Remove(key));

        // Set up HttpContext with session support
        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.SetupGet(c => c.Session).Returns(sessionMock.Object);

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        contextAccessorMock.SetupGet(a => a.HttpContext).Returns(httpContextMock.Object);

        // Create factory with session support
        var gateway = new Mock<IGateway>();
        var options = new Mock<IOptions<InertiaOptions>>();
        options.SetupGet(x => x.Value).Returns(new InertiaOptions());
        var factoryWithSession = new ResponseFactory(contextAccessorMock.Object, gateway.Object, options.Object);

        // Simulate first request: set clearHistory and redirect
        factoryWithSession.ClearHistory();

        // Simulate second request after redirect: create new response
        var response = factoryWithSession.Render("User/Edit", new { });

        var headers = new HeaderDictionary
        {
            { "X-Inertia", "true" }
        };

        var context = PrepareContextWithSession(headers, sessionMock.Object);

        response.SetContext(context);
        await response.ProcessResponse();

        var result = response.GetResult();

        // Assert: clearHistory should persist through redirect
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<JsonResult>());

            var json = (result as JsonResult)?.Value;
            Assert.That(json, Is.InstanceOf<Page>());

            Assert.That((json as Page)?.ClearHistory, Is.EqualTo(true));
            Assert.That((json as Page)?.EncryptHistory, Is.EqualTo(false));
            Assert.That((json as Page)?.Component, Is.EqualTo("User/Edit"));
        });

        // Verify session value was removed after being read (one-time use behavior)
        sessionMock.Verify(s => s.Remove("inertia.clear_history"), Times.Once);
    }

    /// <summary>
    /// Prepares ActionContext with session support for testing redirect scenarios.
    /// </summary>
    private static ActionContext PrepareContextWithSession(HeaderDictionary? headers, ISession session)
    {
        var request = new Mock<HttpRequest>();
        request.SetupGet(r => r.Headers).Returns(headers ?? new HeaderDictionary());

        var response = new Mock<HttpResponse>();
        response.SetupGet(r => r.Headers).Returns(new HeaderDictionary());

        var features = new Microsoft.AspNetCore.Http.Features.FeatureCollection();

        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(c => c.Request).Returns(request.Object);
        httpContext.SetupGet(c => c.Response).Returns(response.Object);
        httpContext.SetupGet(c => c.Features).Returns(features);
        httpContext.SetupGet(c => c.Session).Returns(session);

        return new ActionContext(httpContext.Object, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
    }
}
