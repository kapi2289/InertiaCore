using InertiaCore.Models;
using Microsoft.AspNetCore.Http;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if the defer data is fetched properly.")]
    public void TestDeferData()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestFunc = new Func<string>(() => "Func"),
            TestDefer = _factory.Defer(() =>
            {
                Assert.Pass();
                return "Defer";
            }),
            TestLazy = _factory.Lazy(() =>
            {
                Assert.Fail();
                return "Lazy";
            })
        });

        var context = PrepareContext();

        response.SetContext(context);
        response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testFunc", "Func" },
            { "testDefer", "Defer" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }

    [Test]
    [Description("Test if the Defer data is fetched properly with specified partial props.")]
    public void TestDeferPartialData()
    {
        var response = _factory.Render("Test/Page", new
        {
            TestFunc = new Func<string>(() => "Func"),
            TestDefer = _factory.Defer(() => "Defer")
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testFunc,testDefer" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testFunc", "Func" },
            { "testDefer", "Defer" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }
}
