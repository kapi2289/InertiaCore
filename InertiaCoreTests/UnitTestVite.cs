using System.IO.Abstractions.TestingHelpers;
using InertiaCore.Extensions;
using InertiaCore.Models;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if the Vite configuration registers properly the service.")]
    public void TestViteConfiguration()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddInertia();

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => builder.Services.AddViteHelper());
            Assert.That(builder.Services.Any(s => s.ServiceType == typeof(IViteBuilder)), Is.True);
        });

        var app = builder.Build();
        Assert.DoesNotThrow(() => app.UseInertia());

        Assert.DoesNotThrow(() => Vite.ReactRefresh());
    }

    [Test]
    [Description("Test if the Vite Helper handles hot module reloading properly.")]
    public void TestHot()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"/wwwroot/hot", new MockFileData("http://127.0.0.1:5174") },
        });
        var options = new Mock<IOptions<ViteOptions>>();
        options.SetupGet(x => x.Value).Returns(new ViteOptions());

        var mock = new Mock<ViteBuilder>(options.Object);
        mock.Object.UseFileSystem(fileSystem);

        var htmlResult = mock.Object.ReactRefresh();

        const string inner =
            "<script type=\"module\">import RefreshRuntime from 'http://127.0.0.1:5174/@react-refresh';" +
            "RefreshRuntime.injectIntoGlobalHook(window);" +
            "window.$RefreshReg$ = () => { };" +
            "window.$RefreshSig$ = () => (type) => type;" +
            "window.__vite_plugin_react_preamble_installed__ = true;</script>";

        Assert.That(htmlResult.ToString(), Is.EqualTo(inner));
    }

    [Test]
    [Description("Test if the Vite Helper handles HMR disabled properly.")]
    public void TestNotHot()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>());
        var options = new Mock<IOptions<ViteOptions>>();
        options.SetupGet(x => x.Value).Returns(new ViteOptions());

        var mock = new Mock<ViteBuilder>(options.Object);
        mock.Object.UseFileSystem(fileSystem);

        Assert.That(mock.Object.ReactRefresh().ToString(), Is.EqualTo("<!-- no hot -->"));
    }

    [Test]
    [Description(
        "Test if the Vite Helper handles generating HTML tags for both JS and CSS from HMR and the manifest properly.")]
    public void TestViteInput()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>());
        var options = new Mock<IOptions<ViteOptions>>();
        options.SetupGet(x => x.Value).Returns(new ViteOptions());

        var mock = new Mock<ViteBuilder>(options.Object);
        mock.Object.UseFileSystem(fileSystem);

        // Missing manifest exception
        Assert.Throws<Exception>(() => mock.Object.Input("app.tsx"));

        fileSystem.AddFile(@"/wwwroot/build/manifest.json", new MockFileData("null"));

        // Null manifest exception
        Assert.Throws<Exception>(() => mock.Object.Input("app.tsx"));

        // Missing info exception
        fileSystem.AddFile(@"/wwwroot/build/manifest.json", new MockFileData("{\"main.tsx\": {}}"));

        Assert.Throws<Exception>(() => mock.Object.Input("app.tsx"));

        // Basic JS File
        fileSystem.AddFile(@"/wwwroot/build/manifest.json",
            new MockFileData("{\"app.tsx\": {\"file\": \"assets/main-19038c6a.js\"}}"));

        var result = mock.Object.Input("app.tsx");
        Assert.That(result.ToString(),
            Is.EqualTo("<script src=\"/build/assets/main-19038c6a.js\" type=\"module\"></script>\n\t"));

        // Basic JS File with CSS import
        fileSystem.AddFile(@"/wwwroot/build/manifest.json",
            new MockFileData("{\"app.tsx\": {\"file\": \"assets/main.js\",\"css\": [\"assets/index.css\"]}}"));

        result = mock.Object.Input("app.tsx");
        Assert.That(result.ToString(),
            Is.EqualTo(
                "<script src=\"/build/assets/main.js\" type=\"module\"></script>\n\t<link href=\"/build/assets/index.css\" rel=\"stylesheet\" />\n\t"));

        // Basic CSS file
        fileSystem.AddFile(@"/wwwroot/build/manifest.json",
            new MockFileData("{\"index.scss\": {\"file\": \"assets/index.css\"}}"));

        result = mock.Object.Input("index.scss");
        Assert.That(result.ToString(), Is.EqualTo("<link href=\"/build/assets/index.css\" rel=\"stylesheet\" />\n\t"));

        // Basic CSS file with custom builder dir
        fileSystem.AddFile(@"/wwwroot/manifest.json",
            new MockFileData("{\"index.scss\": {\"file\": \"assets/index.css\"}}"));
        options.SetupGet(x => x.Value).Returns(new ViteOptions
        {
            BuildDirectory = null
        });
        result = mock.Object.Input("index.scss");
        Assert.That(result.ToString(), Is.EqualTo("<link href=\"/assets/index.css\" rel=\"stylesheet\" />\n\t"));

        // Hot file with css import
        options.SetupGet(x => x.Value).Returns(new ViteOptions
        {
            BuildDirectory = "build"
        });
        fileSystem.AddFile(@"/wwwroot/hot", new MockFileData("http://127.0.0.1:5174"));

        result = mock.Object.Input("index.scss");
        Assert.That(result.ToString(), Is.EqualTo(
            "<script src=\"http://127.0.0.1:5174/@vite/client\" type=\"module\"></script>\n\t" +
            "<script src=\"http://127.0.0.1:5174/index.scss\" type=\"module\"></script>\n\t"));

        // Test null build directory
        fileSystem.AddFile(@"/wwwroot/hot", new MockFileData("http://127.0.0.1:5174"));
        options.SetupGet(x => x.Value).Returns(new ViteOptions
        {
            BuildDirectory = null
        });
        result = mock.Object.Input("index.scss");
        Assert.That(result.ToString(), Is.EqualTo(
            "<script src=\"http://127.0.0.1:5174/@vite/client\" type=\"module\"></script>\n\t" +
            "<script src=\"http://127.0.0.1:5174/index.scss\" type=\"module\"></script>\n\t"));

        // Test empty build directory
        options.SetupGet(x => x.Value).Returns(new ViteOptions
        {
            BuildDirectory = ""
        });
        result = mock.Object.Input("index.scss");
        Assert.That(result.ToString(), Is.EqualTo(
            "<script src=\"http://127.0.0.1:5174/@vite/client\" type=\"module\"></script>\n\t" +
            "<script src=\"http://127.0.0.1:5174/index.scss\" type=\"module\"></script>\n\t"));

        // Basic JS File via hot
        result = mock.Object.Input("app.tsx");
        Assert.That(result.ToString(), Is.EqualTo(
            "<script src=\"http://127.0.0.1:5174/@vite/client\" type=\"module\"></script>\n\t" +
            "<script src=\"http://127.0.0.1:5174/app.tsx\" type=\"module\"></script>\n\t"));
    }

    [Test]
    [Description("Test if the Vite Facade behaves correctly with different builder configurations.")]
    public void TestViteFacade()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>());
        var options = new Mock<IOptions<ViteOptions>>();
        options.SetupGet(x => x.Value).Returns(new ViteOptions
        {
            BuildDirectory = "build2",
            ManifestFilename = "manifest-test.json",
            HotFile = "cold",
            PublicDirectory = "public"
        });

        var mock = new Mock<ViteBuilder>(options.Object);
        mock.Object.UseFileSystem(fileSystem);

        Vite.UseBuilder(mock.Object);

        // Basic Manifest with CSS file
        fileSystem.AddFile(@"/public/build2/manifest-test.json",
            new MockFileData("{\"index.scss\": {\"file\": \"assets/index.css\"}}"));
        var result = Vite.Input("index.scss");
        Assert.That(result.ToString(), Is.EqualTo("<link href=\"/build2/assets/index.css\" rel=\"stylesheet\" />\n\t"));

        // Hot file with css import
        options.SetupGet(x => x.Value).Returns(new ViteOptions
        {
            BuildDirectory = null,
            ManifestFilename = "manifest-test.json",
            HotFile = "cold",
            PublicDirectory = "public"
        });
        fileSystem.AddFile(@"/public/cold", new MockFileData("http://127.0.0.1:5174"));
        result = Vite.Input("index.scss");
        Assert.That(result.ToString(), Is.EqualTo(
            "<script src=\"http://127.0.0.1:5174/@vite/client\" type=\"module\"></script>\n\t" +
            "<script src=\"http://127.0.0.1:5174/index.scss\" type=\"module\"></script>\n\t"));
    }

    [Test]
    [Description("Test if the vite version is read properly.")]
    public async Task TestViteVersion()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>());
        var options = new Mock<IOptions<ViteOptions>>();
        options.SetupGet(x => x.Value).Returns(new ViteOptions());

        var mock = new Mock<ViteBuilder>(options.Object);
        mock.Object.UseFileSystem(fileSystem);
        Vite.UseBuilder(mock.Object);

        fileSystem.AddFile(@"/wwwroot/build/manifest.json",
            new MockFileData("{\"app.tsx\": {\"file\": \"assets/main-19038c6a.js\"}}"));

        _factory.Version(Vite.GetManifestHash);

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
            Assert.That((json as Page)?.Version, Is.EqualTo("bba1afd1066309f4a69430e0c446ba8d"));
        });
    }
}
