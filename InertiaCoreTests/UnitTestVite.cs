using System.IO.Abstractions.TestingHelpers;
using InertiaCore.Utils;
using Moq;

namespace InertiaCoreTests;

public partial class Tests
{

    [Test]
    public void TestHot()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"/wwwroot/build/hot", new MockFileData("http://127.0.0.1:5174") },
        });

        var mock = new Mock<ViteBuilder>(fileSystem);

        var htmlResult = mock.Object.reactRefresh();
        mock.Object.usePublicDirectory(@"wwwroot");

        Assert.That(htmlResult.ToString(), Is.Not.EqualTo("<!-- no hot -->"));

        htmlResult = mock.Object.reactRefresh();

        var inner = "<script type=\"module\">import RefreshRuntime from 'http://127.0.0.1:5174/@react-refresh';" +
            "RefreshRuntime.injectIntoGlobalHook(window);" +
            "window.$RefreshReg$ = () => { };" +
            "window.$RefreshSig$ = () => (type) => type;" +
            "window.__vite_plugin_react_preamble_installed__ = true;</script>";

        Assert.AreEqual(htmlResult.ToString(), inner);
    }

    [Test]
    public void TestNotHot()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData> { });

        var mock = new Mock<ViteBuilder>(fileSystem);

        var htmlResult = mock.Object.reactRefresh();

        Assert.That(htmlResult.ToString(), Is.EqualTo("<!-- no hot -->"));

        Assert.That(Vite.reactRefresh().ToString(), Is.EqualTo("<!-- no hot -->"));
    }

    [Test]
    public void TestViteInput()
    {

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData> { });
        var mock = new Mock<ViteBuilder>(fileSystem);

        // Missing manifest exception
        Assert.Throws<Exception>(() => mock.Object.input("app.tsx"));

        fileSystem.AddFile(@"/wwwroot/build/manifest.json", new MockFileData("null"));

        // Null manifest exception
        Assert.Throws<Exception>(() => mock.Object.input("app.tsx"));

        // Missing info exception
        fileSystem.AddFile(@"/wwwroot/build/manifest.json", new MockFileData("{\"main.tsx\": {}}"));

        Assert.Throws<Exception>(() => mock.Object.input("app.tsx"));

        // Basic JS File
        fileSystem.AddFile(@"/wwwroot/build/manifest.json", new MockFileData("{\"app.tsx\": {\"file\": \"assets/main-19038c6a.js\"}}"));

        var result = mock.Object.input("app.tsx");
        Assert.AreEqual(result.ToString(), "<script src=\"/build/assets/main-19038c6a.js\" type=\"text/javascript\"></script>\n\t");

        // Basic JS File with CSS import
        fileSystem.AddFile(@"/wwwroot/build/manifest.json", new MockFileData("{\"app.tsx\": {\"file\": \"assets/main.js\",\"css\": [\"assets/index.css\"]}}"));

        result = mock.Object.input("app.tsx");
        Assert.AreEqual(result.ToString(), "<script src=\"/build/assets/main.js\" type=\"text/javascript\"></script>\n\t<link href=\"/build/assets/index.css\" rel=\"stylesheet\" />\n\t");

        // Basic CSS file
        fileSystem.AddFile(@"/wwwroot/build/manifest.json", new MockFileData("{\"index.scss\": {\"file\": \"assets/index.css\"}}"));

        result = mock.Object.input("index.scss");
        Assert.AreEqual(result.ToString(), "<link href=\"/build/assets/index.css\" rel=\"stylesheet\" />\n\t");

        // Hot file with css import
        fileSystem.AddFile(@"/wwwroot/build/hot", new MockFileData("http://127.0.0.1:5174"));

        result = mock.Object.input("index.scss");
        Assert.AreEqual(result.ToString(), "<script src=\"http://127.0.0.1:5174/@vite/client\" type=\"module\"></script>\n\t" +
            "<script src=\"http://127.0.0.1:5174/index.scss\" type=\"module\"></script>\n\t");
    }

}
