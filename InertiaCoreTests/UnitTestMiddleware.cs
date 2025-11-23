using System.Net;
using InertiaCore;
using InertiaCore.Models;
using InertiaCore.Ssr;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Moq;

namespace InertiaCoreTests;

[TestFixture]
public class UnitTestMiddleware
{
    private Middleware _middleware = null!;
    private Mock<RequestDelegate> _nextMock = null!;
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<ITempDataDictionaryFactory> _tempDataFactoryMock = null!;
    private Mock<ITempDataDictionary> _tempDataMock = null!;
    private Mock<IInertiaSerializer> _serializerMock = null!;
    private IResponseFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        _nextMock = new Mock<RequestDelegate>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _tempDataFactoryMock = new Mock<ITempDataDictionaryFactory>();
        _tempDataMock = new Mock<ITempDataDictionary>();
        _serializerMock = new Mock<IInertiaSerializer>();

        _tempDataFactoryMock.Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
            .Returns(_tempDataMock.Object);

        _serviceProviderMock.Setup(s => s.GetService(typeof(ITempDataDictionaryFactory)))
            .Returns(_tempDataFactoryMock.Object);

        // Set up Inertia factory
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var gateway = new Gateway(httpClientFactory.Object, _serializerMock.Object);
        var options = new Mock<IOptions<InertiaOptions>>();
        options.SetupGet(x => x.Value).Returns(new InertiaOptions());

        _factory = new ResponseFactory(contextAccessor.Object, gateway, _serializerMock.Object, options.Object);
        Inertia.UseFactory(_factory);

        _middleware = new Middleware(_nextMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        // Reset the static factory to not interfere with other tests
        Inertia.ResetFactory();
    }

    [Test]
    public async Task InvokeAsync_NonInertiaRequest_CallsNext()
    {
        // Arrange
        var context = CreateHttpContext(isInertia: false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(next => next(context), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_InertiaPostRequest_CallsNext()
    {
        // Arrange
        var context = CreateHttpContext(
            isInertia: true,
            method: "POST",
            version: "test-version"
        );
        Inertia.Version("test-version");

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(next => next(context), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_InertiaGetRequestWithSameVersion_CallsNext()
    {
        // Arrange
        const string version = "v1.0.0";
        Inertia.Version(version);
        var context = CreateHttpContext(
            isInertia: true,
            method: "GET",
            version: version
        );

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(next => next(context), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_InertiaGetRequestWithDifferentVersion_ReturnsConflict()
    {
        // Arrange
        const string currentVersion = "v2.0.0";
        const string requestVersion = "v1.0.0";
        Inertia.Version(currentVersion);

        var context = CreateHttpContext(
            isInertia: true,
            method: "GET",
            version: requestVersion,
            requestUri: "https://example.com/test"
        );

        // Setup ITempDataDictionary to indicate no temp data
        _tempDataMock.Setup(t => t.Count).Returns(0);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.That(context.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.Conflict));
        Assert.That(context.Response.Headers[InertiaHeader.Location].ToString(), Is.EqualTo("/test"));
        _nextMock.Verify(next => next(It.IsAny<HttpContext>()), Times.Never);
    }

    [Test]
    public async Task InvokeAsync_VersionChangeWithTempData_KeepsTempData()
    {
        // Arrange
        const string currentVersion = "v2.0.0";
        const string requestVersion = "v1.0.0";
        Inertia.Version(currentVersion);

        var context = CreateHttpContext(
            isInertia: true,
            method: "GET",
            version: requestVersion,
            requestUri: "https://example.com/test"
        );

        // Setup ITempDataDictionary to indicate it has temp data
        _tempDataMock.Setup(t => t.Count).Returns(1);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _tempDataMock.Verify(t => t.Keep(), Times.Once);
        Assert.That(context.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.Conflict));
    }

    [Test]
    public async Task InvokeAsync_VersionChangeWithoutTempData_DoesNotKeepTempData()
    {
        // Arrange
        const string currentVersion = "v2.0.0";
        const string requestVersion = "v1.0.0";
        Inertia.Version(currentVersion);

        var context = CreateHttpContext(
            isInertia: true,
            method: "GET",
            version: requestVersion,
            requestUri: "https://example.com/test"
        );

        // Setup ITempDataDictionary to indicate no temp data
        _tempDataMock.Setup(t => t.Count).Returns(0);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _tempDataMock.Verify(t => t.Keep(), Times.Never);
        Assert.That(context.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.Conflict));
    }

    [Test]
    public async Task InvokeAsync_InertiaGetRequestWithNoVersionHeader_CallsNext()
    {
        // Arrange
        var context = CreateHttpContext(
            isInertia: true,
            method: "GET",
            version: null
        );

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(next => next(context), Times.Once);
    }

    private HttpContext CreateHttpContext(
        bool isInertia = false,
        string method = "GET",
        string? version = null,
        string requestUri = "https://example.com")
    {
        var requestHeaders = new HeaderDictionary();
        if (isInertia)
        {
            requestHeaders[InertiaHeader.Inertia] = "true";
        }

        if (version != null)
        {
            requestHeaders[InertiaHeader.Version] = version;
        }

        var requestMock = new Mock<HttpRequest>();
        requestMock.SetupGet(r => r.Method).Returns(method);
        requestMock.SetupGet(r => r.Scheme).Returns("https");
        requestMock.SetupGet(r => r.Host).Returns(new HostString("example.com"));
        requestMock.SetupGet(r => r.Path).Returns(new Uri(requestUri).AbsolutePath);
        requestMock.SetupGet(r => r.QueryString).Returns(new QueryString(new Uri(requestUri).Query));
        requestMock.SetupGet(r => r.Headers).Returns(requestHeaders);

        var responseHeaders = new HeaderDictionary();
        var responseBody = new MemoryStream();
        var responseMock = new Mock<HttpResponse>();
        responseMock.SetupGet(r => r.Headers).Returns(responseHeaders);
        responseMock.SetupProperty(r => r.StatusCode);
        responseMock.SetupGet(r => r.Body).Returns(responseBody);

        var contextMock = new Mock<HttpContext>();
        contextMock.SetupGet(c => c.Request).Returns(requestMock.Object);
        contextMock.SetupGet(c => c.Response).Returns(responseMock.Object);
        contextMock.SetupGet(c => c.RequestServices).Returns(_serviceProviderMock.Object);

        return contextMock.Object;
    }
}
