using InertiaCore.Models;
using Microsoft.AspNetCore.Http;
using System.Reflection.Metadata.Ecma335;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if the lazy data is fetched properly.")]
    public void TestLazyData()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestFunc = new Func<string>(() => "Func"),
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
            { "errors", new Dictionary<string, string>(0) }
        }));
    }

    [Test]
    [Description("Test if the lazy data is fetched properly with specified partial props.")]
    public void TestLazyPartialData()
    {
        var response = _factory.Render("Test/Page", new
        {
            TestFunc = new Func<string>(() => "Func"),
            TestLazy = _factory.Lazy(() => "Lazy")
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testFunc,testLazy" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testFunc", "Func" },
            { "testLazy", "Lazy" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }


    [Test]
    [Description("Test if the lazy async data is fetched properly.")]
    public void TestLazyAsyncData()
    {
        var testFunction = new Func<Task<object?>>(async () =>
        {
            Assert.Fail();
            await Task.Delay(100);
            return "Lazy Async";
        });

        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestFunc = new Func<string>(() => "Func"),
            TestLazy = _factory.LazyAsync(testFunction)
        });

        var context = PrepareContext();

        response.SetContext(context);
        response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testFunc", "Func" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }

    [Test]
    [Description("Test if the lazy async data is fetched properly with specified partial props.")]
    public void TestLazyAsyncPartialData()
    {
        var testFunction = new Func<Task<object?>>(async () =>
        {
            await Task.Delay(100);
            return "Lazy Async";
        });

        var response = _factory.Render("Test/Page", new
        {
            TestFunc = new Func<string>(() => "Func"),
            TestLazy = _factory.LazyAsync(testFunction)
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testFunc,testLazy" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testFunc", "Func" },
            { "testLazy", "Lazy Async" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }

}
