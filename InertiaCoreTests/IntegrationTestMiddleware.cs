using InertiaCore;
using InertiaCore.Extensions;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Text.Json;
using System.Text;

namespace InertiaCoreTests;

[TestFixture]
public class IntegrationTestMiddleware
{
    private TestServer _server = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void Setup()
    {
        // Create test server with Inertia middleware
        var builder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddInertia();
                    services.AddMvc();
                    services.AddDistributedMemoryCache(); // Required for TempData
                    services.AddSession(); // Required for TempData
                });
                webHost.Configure(app =>
                {
                    // This calls UseInertia which should register the middleware
                    app.UseInertia();
                    app.UseSession(); // Enable session middleware for TempData
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/test",
                            async context => { await context.Response.WriteAsync("Hello from endpoint"); });

                        endpoints.MapPost("/empty", context =>
                        {
                            // Return empty response (no content written)
                            context.Response.StatusCode = 200;
                            context.Response.ContentLength = 0;
                            // Intentionally don't write anything to simulate empty response
                            return Task.CompletedTask;
                        });
                    });
                });
            });

        var host = builder.Start();
        _server = host.GetTestServer();
        _client = _server.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _server?.Dispose();

        // Reset the static factory to not interfere with other tests
        Inertia.ResetFactory();
    }

    [Test]
    public async Task Middleware_IsRegistered_WhenInertiaRequestWithVersionMismatch_Returns409()
    {
        // Arrange
        Inertia.Version("v2.0.0");
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add(InertiaHeader.Inertia, "true");
        request.Headers.Add(InertiaHeader.Version, "v1.0.0");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(response.Headers.Contains(InertiaHeader.Location), Is.True);
        Assert.That(response.Headers.GetValues(InertiaHeader.Location).First(), Is.EqualTo("/test"));
    }

    [Test]
    public async Task Middleware_IsRegistered_WhenNonInertiaRequest_PassesThrough()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");

        // Act
        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(content, Is.EqualTo("Hello from endpoint"));
    }

    [Test]
    public async Task Middleware_IsRegistered_WhenInertiaRequestWithSameVersion_PassesThrough()
    {
        // Arrange
        Inertia.Version("v1.0.0");
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add(InertiaHeader.Inertia, "true");
        request.Headers.Add(InertiaHeader.Version, "v1.0.0");

        // Act
        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(content, Is.EqualTo("Hello from endpoint"));
    }

    [Test]
    public async Task Middleware_IsRegistered_WhenInertiaPostRequest_PassesThrough()
    {
        // Arrange
        Inertia.Version("v2.0.0");
        var request = new HttpRequestMessage(HttpMethod.Post, "/test");
        request.Headers.Add(InertiaHeader.Inertia, "true");
        request.Headers.Add(InertiaHeader.Version, "v1.0.0"); // Different version

        // Act
        var response = await _client.SendAsync(request);

        // Assert - POST should pass through even with version mismatch
        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task Middleware_HandlesMultipleRequests_WithDifferentVersions()
    {
        // First request with matching version
        Inertia.Version("v1.0.0");
        var request1 = new HttpRequestMessage(HttpMethod.Get, "/test");
        request1.Headers.Add(InertiaHeader.Inertia, "true");
        request1.Headers.Add(InertiaHeader.Version, "v1.0.0");

        var response1 = await _client.SendAsync(request1);
        Assert.That(response1.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Change version and send request with old version
        Inertia.Version("v2.0.0");
        var request2 = new HttpRequestMessage(HttpMethod.Get, "/test");
        request2.Headers.Add(InertiaHeader.Inertia, "true");
        request2.Headers.Add(InertiaHeader.Version, "v1.0.0");

        var response2 = await _client.SendAsync(request2);
        Assert.That(response2.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task Middleware_HandlesEmptyResponse_RedirectsToDefault()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/empty");
        request.Headers.Add(InertiaHeader.Inertia, "true");

        // Act
        var response = await _client.SendAsync(request);

        // Assert - Should redirect back to default since no referrer is available in test
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.SeeOther));
        Assert.That(response.Headers.Location?.ToString(), Is.EqualTo("/"));
    }


    [Test]
    public async Task Middleware_NonInertiaEmptyResponse_DoesNotRedirect()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/empty");
        // No Inertia header

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Headers.Location, Is.Null);
    }

    // Simple logger implementation that captures messages
    public class TestLogger : ILogger<IApplicationBuilder>
    {
        public List<string> LoggedMessages { get; } = new List<string>();

        IDisposable ILogger.BeginScope<TState>(TState state) => null!;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            LoggedMessages.Add(message);
        }
    }

    [Test]
    public void UseInertia_WithoutTempDataServices_LogsWarning()
    {
        // Arrange
        var testLogger = new TestLogger();

        var builder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.UseEnvironment("Development"); // Use Development environment to bypass test suppression
                webHost.ConfigureServices(services =>
                {
                    services.AddInertia();
                    services.AddRouting(); // Minimal routing services
                    // Intentionally NOT adding AddMvc() or AddSession() to trigger the warning
                    // Replace the default logger with our test logger
                    services.AddSingleton<ILogger<IApplicationBuilder>>(testLogger);
                });
                webHost.Configure(app =>
                {
                    app.UseInertia(); // This should trigger the warning
                });
            });

        // Act
        var host = builder.Start();

        // Assert
        Assert.That(testLogger.LoggedMessages.Any(msg => msg.Contains("TempData services are not configured")), Is.True,
            $"Expected warning message not found. Logged messages: {string.Join(", ", testLogger.LoggedMessages)}");

        host.Dispose();
    }
}
