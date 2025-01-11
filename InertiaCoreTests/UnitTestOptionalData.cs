using InertiaCore.Models;
using Microsoft.AspNetCore.Http;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if the optional data is fetched properly.")]
    public async Task TestOptionalData()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestFunc = new Func<string>(() => "Func"),
            TestOptional = _factory.Optional(() =>
            {
                Assert.Fail();
                return "Optional";
            })
        });

        var context = PrepareContext();

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testFunc", "Func" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }

    [Test]
    [Description("Test if the optional data is fetched properly with specified partial props.")]
    public async Task TestOptionalPartialData()
    {
        var response = _factory.Render("Test/Page", new
        {
            TestFunc = new Func<string>(() => "Func"),
            TestOptional = _factory.Optional(() => "Optional")
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testFunc,testOptional" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testFunc", "Func" },
            { "testOptional", "Optional" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }


    [Test]
    [Description("Test if the optional async data is fetched properly.")]
    public async Task TestOptionalAsyncData()
    {
        var testFunction = new Func<Task<object?>>(async () =>
        {
            Assert.Fail();
            await Task.Delay(100);
            return "Optional Async";
        });

        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestFunc = new Func<string>(() => "Func"),
            TestOptional = _factory.Optional(testFunction)
        });

        var context = PrepareContext();

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testFunc", "Func" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }

    [Test]
    [Description("Test if the optional async data is fetched properly with specified partial props.")]
    public async Task TestOptionalAsyncPartialData()
    {
        var testFunction = new Func<Task<string>>(async () =>
        {
            await Task.Delay(100);
            return "Optional Async";
        });

        var response = _factory.Render("Test/Page", new
        {
            TestFunc = new Func<string>(() => "Func"),
            TestOptional = _factory.Optional(async () => await testFunction())
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testFunc,testOptional" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testFunc", "Func" },
            { "testOptional", "Optional Async" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }
}
