using InertiaCore;
using InertiaCore.Models;
using InertiaCore.Ssr;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test component exists validation is disabled by default")]
    public void TestComponentValidationDisabledByDefault()
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

        var gateway = new Gateway(httpClientFactory.Object);
        var options = new Mock<IOptions<InertiaOptions>>();
        options.SetupGet(x => x.Value).Returns(new InertiaOptions { EnsurePagesExist = false });

        var factory = new ResponseFactory(contextAccessor.Object, gateway, options.Object, environment.Object);

        Assert.DoesNotThrow(() => factory.Render("NonexistentComponent"));
    }

    [Test]
    [Description("Test component validation throws exception when enabled and component doesn't exist")]
    public void TestComponentValidationThrowsWhenComponentMissing()
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

        var gateway = new Gateway(httpClientFactory.Object);
        var options = new Mock<IOptions<InertiaOptions>>();
        options.SetupGet(x => x.Value).Returns(new InertiaOptions { EnsurePagesExist = true });

        var factory = new ResponseFactory(contextAccessor.Object, gateway, options.Object, environment.Object);

        var ex = Assert.Throws<ComponentNotFoundException>(() => factory.Render("NonexistentComponent"));
        Assert.That(ex.Component, Is.EqualTo("NonexistentComponent"));
        Assert.That(ex.Message, Contains.Substring("NonexistentComponent"));
    }

    [Test]
    [Description("Test component validation passes when component exists")]
    public void TestComponentValidationPassesWhenComponentExists()
    {
        var tempDir = Path.GetTempPath();
        var pagesDir = Path.Combine(tempDir, "src", "Pages");
        Directory.CreateDirectory(pagesDir);

        var testComponent = Path.Combine(pagesDir, "TestComponent.tsx");
        File.WriteAllText(testComponent, "export default function TestComponent() { return <div>Test</div>; }");

        try
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(x => x.ContentRootPath).Returns(tempDir);

            var gateway = new Gateway(httpClientFactory.Object);
            var options = new Mock<IOptions<InertiaOptions>>();
            options.SetupGet(x => x.Value).Returns(new InertiaOptions
            {
                EnsurePagesExist = true,
                PagePaths = new[] { "~/src/Pages" },
                PageExtensions = new[] { ".tsx" }
            });

            var factory = new ResponseFactory(contextAccessor.Object, gateway, options.Object, environment.Object);

            Assert.DoesNotThrow(() => factory.Render("TestComponent"));
        }
        finally
        {
            if (File.Exists(testComponent))
                File.Delete(testComponent);
            if (Directory.Exists(pagesDir))
                Directory.Delete(pagesDir, true);
        }
    }

    [Test]
    [Description("Test component validation works with nested paths")]
    public void TestComponentValidationWithNestedPaths()
    {
        var tempDir = Path.GetTempPath();
        var pagesDir = Path.Combine(tempDir, "ClientApp", "src", "Pages", "Auth");
        Directory.CreateDirectory(pagesDir);

        var testComponent = Path.Combine(pagesDir, "Login.vue");
        File.WriteAllText(testComponent, "<template><div>Login</div></template>");

        try
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(x => x.ContentRootPath).Returns(tempDir);

            var gateway = new Gateway(httpClientFactory.Object);
            var options = new Mock<IOptions<InertiaOptions>>();
            options.SetupGet(x => x.Value).Returns(new InertiaOptions
            {
                EnsurePagesExist = true,
                PagePaths = new[] { "~/ClientApp/src/Pages" },
                PageExtensions = new[] { ".vue" }
            });

            var factory = new ResponseFactory(contextAccessor.Object, gateway, options.Object, environment.Object);

            Assert.DoesNotThrow(() => factory.Render("Auth/Login"));
        }
        finally
        {
            if (File.Exists(testComponent))
                File.Delete(testComponent);
            if (Directory.Exists(pagesDir))
                Directory.Delete(pagesDir, true);
        }
    }
}