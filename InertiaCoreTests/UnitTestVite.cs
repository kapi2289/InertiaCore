using InertiaCore.Utils;
using Moq;
using Moq.Protected;

namespace InertiaCoreTests;

public partial class Tests : ViteBuilder
{

    [Test]
    public void TestHot()
    {

        var mock = new Mock<ViteBuilder>();

        var htmlResult = mock.Object.reactRefresh();

        Assert.AreEqual(htmlResult.ToString(), "<!-- no hot -->");

        Assert.AreEqual(Vite.reactRefresh().ToString(), "<!-- no hot -->");

        mock.Protected()
            .Setup<bool>("isRunningHot")
            .Returns(true);

        mock.Protected()
            .Setup<string>("hotAsset", ItExpr.IsAny<string>())
            .Returns((string path) => { return "http://127.0.0.1:5174/" + path; });

        htmlResult = mock.Object.reactRefresh();

        var inner = "<script type=\"module\">import RefreshRuntime from 'http://127.0.0.1:5174/@react-refresh';" +
            "RefreshRuntime.injectIntoGlobalHook(window);" +
            "window.$RefreshReg$ = () => { };" +
            "window.$RefreshSig$ = () => (type) => type;" +
            "window.__vite_plugin_react_preamble_installed__ = true;</script>";

        Assert.AreEqual(htmlResult.ToString(), inner);
    }

    [Test]
    public void TestViteFacade()
    {

        Vite.useBuildDir("build2");

        Assert.AreEqual(ViteBuilder.Instance.buildDirectory, "build2");

        Vite.useManifestFilename("manifest-test.json");

        Assert.AreEqual(ViteBuilder.Instance.manifestFilename, "manifest-test.json");

        Vite.useHotFile("cold");

        Assert.AreEqual(ViteBuilder.Instance.hotFile, "cold");

        Vite.usePublicDir("public");

        Assert.AreEqual(ViteBuilder.Instance.publicDirectory, "public");

        Assert.True(GetPublicDir("file.css") == "public/build2/file.css");

        Vite.useBuildDir(null);

        Assert.True(GetPublicDir("file.css") == "public/file.css");

        Vite.useBuildDir("");

        Assert.True(GetPublicDir("file.css") == "public/file.css");
    }

    [Test]
    public void TestViteBuilderHelpers()
    {

        var mock = new Mock<ViteBuilder>();

        mock.Protected()
            .Setup<bool>("isRunningHot")
            .Returns(false);

        Assert.AreEqual(mock.Object.asset("manifest.json"), "/build/manifest.json");

        mock.Object.buildDirectory = null;

        Assert.AreEqual(mock.Object.asset("manifest.json"), "/manifest.json");

        mock.Protected()
            .Setup<bool>("isRunningHot")
            .Returns(true);

        mock.Protected()
            .Setup<string>("readFile", ItExpr.IsAny<string>())
            .Returns("http://127.0.0.1:5174");

        Assert.AreEqual(mock.Object.asset("manifest.json"), null);
    }

    [Test]
    public void TestViteInput()
    {

        var mock = new Mock<ViteBuilder>();

        Assert.Throws<Exception>(() => mock.Object.input("app.tsx"));

        mock.Protected()
            .Setup<bool>("exists", ItExpr.IsAny<string>())
            .Returns(true);

        mock.Protected()
            .Setup<string>("readFile", ItExpr.IsAny<string>())
            .Returns("null");

        Assert.Throws<Exception>(() => mock.Object.input("app.tsx"));

        mock.Protected()
            .Setup<string>("readFile", ItExpr.IsAny<string>())
            .Returns("{\"main.tsx\": {}}");

        Assert.Throws<Exception>(() => mock.Object.input("app.tsx"));

        mock.Protected()
            .Setup<string>("readFile", ItExpr.IsAny<string>())
            .Returns("{\"app.tsx\": {\"file\": \"assets/main-19038c6a.js\"}}");

        var result = mock.Object.input("app.tsx");
        Assert.AreEqual(result.ToString(), "<script src=\"/build/assets/main-19038c6a.js\" type=\"text/javascript\"></script>\n\t");

        mock.Protected()
            .Setup<string>("readFile", ItExpr.IsAny<string>())
            .Returns("{\"app.tsx\": {\"file\": \"assets/main.js\",\"css\": [\"assets/index.css\"]}}");

        result = mock.Object.input("app.tsx");
        Assert.AreEqual(result.ToString(), "<script src=\"/build/assets/main.js\" type=\"text/javascript\"></script>\n\t<link href=\"/build/assets/index.css\" rel=\"stylesheet\" />\n\t");

        mock.Protected()
            .Setup<string>("readFile", ItExpr.IsAny<string>())
            .Returns("{\"index.scss\": {\"file\": \"assets/index.css\"}}");

        result = mock.Object.input("index.scss");
        Assert.AreEqual(result.ToString(), "<link href=\"/build/assets/index.css\" rel=\"stylesheet\" />\n\t");

        mock.Protected()
            .Setup<bool>("isRunningHot")
            .Returns(true);

        mock.Protected()
            .Setup<string>("hotAsset", ItExpr.IsAny<string>())
            .Returns((string path) => { return "http://127.0.0.1:5174/" + path; });

        result = mock.Object.input("index.scss");
        Assert.AreEqual(result.ToString(), "<script src=\"http://127.0.0.1:5174/@vite/client\" type=\"module\"></script>\n\t" +
            "<script src=\"http://127.0.0.1:5174/index.scss\" type=\"module\"></script>\n\t");
    }

    public string GetPublicDir(string path)
    {
        return base.getPublicDir(path);
    }
}
