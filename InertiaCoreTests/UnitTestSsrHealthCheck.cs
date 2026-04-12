using System.Net;
using InertiaCore.Models;
using InertiaCore.Ssr;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test SSR health check returns true when server responds with success")]
    public async Task TestSsrHealthCheckReturnsTrue()
    {
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

        var options = new Mock<IOptions<InertiaOptions>>();
        options.SetupGet(x => x.Value).Returns(new InertiaOptions { SsrUrl = "http://localhost:13714" });

        var gateway = new Gateway(httpClientFactoryMock.Object, options.Object, environment.Object);

        var result = await gateway.IsHealthy();

        Assert.That(result, Is.True);
        httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.ToString() == "http://localhost:13714/health"),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Test]
    [Description("Test SSR health check returns false when server responds with error")]
    public async Task TestSsrHealthCheckReturnsFalseOnError()
    {
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

        var options = new Mock<IOptions<InertiaOptions>>();
        options.SetupGet(x => x.Value).Returns(new InertiaOptions { SsrUrl = "http://localhost:13714" });

        var gateway = new Gateway(httpClientFactoryMock.Object, options.Object, environment.Object);

        var result = await gateway.IsHealthy();

        Assert.That(result, Is.False);
    }

    [Test]
    [Description("Test SSR health check returns false when request throws exception")]
    public async Task TestSsrHealthCheckReturnsFalseOnException()
    {
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

        var options = new Mock<IOptions<InertiaOptions>>();
        options.SetupGet(x => x.Value).Returns(new InertiaOptions { SsrUrl = "http://localhost:13714" });

        var gateway = new Gateway(httpClientFactoryMock.Object, options.Object, environment.Object);

        var result = await gateway.IsHealthy();

        Assert.That(result, Is.False);
    }

    [Test]
    [Description("Test SSR health check constructs correct URL with trailing slash")]
    public async Task TestSsrHealthCheckUrlWithTrailingSlash()
    {
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

        var options = new Mock<IOptions<InertiaOptions>>();
        options.SetupGet(x => x.Value).Returns(new InertiaOptions { SsrUrl = "http://localhost:13714/" });

        var gateway = new Gateway(httpClientFactoryMock.Object, options.Object, environment.Object);

        await gateway.IsHealthy();

        httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.ToString() == "http://localhost:13714/health"),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Test]
    [Description("Test SSR health check works with different port")]
    public async Task TestSsrHealthCheckWithDifferentPort()
    {
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

        var options = new Mock<IOptions<InertiaOptions>>();
        options.SetupGet(x => x.Value).Returns(new InertiaOptions { SsrUrl = "http://127.0.0.1:8080/ssr" });

        var gateway = new Gateway(httpClientFactoryMock.Object, options.Object, environment.Object);

        await gateway.IsHealthy();

        httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.ToString() == "http://127.0.0.1:8080/ssr/health"),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}