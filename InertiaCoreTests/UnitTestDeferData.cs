using InertiaCore.Models;
using Microsoft.AspNetCore.Http;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if the defer data is fetched properly.")]
    public async Task TestDeferData()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestFunc = new Func<string>(() => "Func"),
            TestDefer = _factory.Defer(() =>
            {
                return "Defer";
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
        Assert.That(page?.DeferredProps, Is.EqualTo(new Dictionary<string, List<string>> {
            { "default", new List<string> { "testDefer" } }
         }));
        Assert.That(page?.MergeProps, Is.EqualTo(null));
    }

    [Test]
    [Description("Test if the defer data is fetched properly with specified partial props.")]
    public async Task TestDeferPartialData()
    {
        var response = _factory.Render("Test/Page", new
        {
            TestFunc = new Func<string>(() => "Func"),
            TestDefer = _factory.Defer(() => "Deferred")
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testFunc,testDefer" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testFunc", "Func" },
            { "testDefer", "Deferred" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        Assert.That(page?.DeferredProps, Is.EqualTo(null));
        Assert.That(page?.MergeProps, Is.EqualTo(null));
    }

    [Test]
    [Description("Test if the defer/merge data is fetched properly with specified partial props.")]
    public async Task TestDeferMergePartialData()
    {
        var response = _factory.Render("Test/Page", new
        {
            TestFunc = new Func<string>(() => "Func"),
            TestDefer = _factory.Defer(() => "Deferred").Merge()
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testFunc,testDefer" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testFunc", "Func" },
            { "testDefer", "Deferred" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        Assert.That(page?.DeferredProps, Is.EqualTo(null));
        Assert.That(page?.MergeProps, Is.EqualTo(new List<string> { "testDefer" }));
    }

    [Test]
    [Description("Test if the defer async data is fetched properly.")]
    public async Task TestDeferAsyncData()
    {
        var testFunction = new Func<Task<object?>>(async () =>
        {
            await Task.Delay(100);
            return "Defer Async";
        });

        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestFunc = new Func<string>(() => "Func"),
            TestDefer = _factory.Defer(testFunction)
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
        Assert.That(page?.DeferredProps, Is.EqualTo(new Dictionary<string, List<string>> {
            { "default", new List<string> { "testDefer" } }
         }));
        Assert.That(page?.MergeProps, Is.EqualTo(null));
    }

    [Test]
    [Description("Test if the defer async data is fetched properly with specified partial props.")]
    public async Task TestDeferAsyncPartialData()
    {
        var testFunction = new Func<Task<string>>(async () =>
        {
            await Task.Delay(100);
            return "Defer Async";
        });

        var response = _factory.Render("Test/Page", new
        {
            TestFunc = new Func<string>(() => "Func"),
            TestDefer = _factory.Defer(async () => await testFunction())
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testFunc,testDefer" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testFunc", "Func" },
            { "testDefer", "Defer Async" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        Assert.That(page?.DeferredProps, Is.EqualTo(null));
        Assert.That(page?.MergeProps, Is.EqualTo(null));
    }

    [Test]
    [Description("Test if the defer & merge async data is fetched properly with specified partial props.")]
    public async Task TestDeferMergeAsyncPartialData()
    {
        var testFunction = new Func<Task<string>>(async () =>
        {
            await Task.Delay(100);
            return "Defer Async";
        });

        var response = _factory.Render("Test/Page", new
        {
            TestFunc = new Func<string>(() => "Func"),
            TestDefer = _factory.Defer(async () => await testFunction()).Merge()
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testFunc,testDefer" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testFunc", "Func" },
            { "testDefer", "Defer Async" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        Assert.That(page?.DeferredProps, Is.EqualTo(null));
        Assert.That(page?.MergeProps, Is.EqualTo(new List<string> { "testDefer" }));
    }

    [Test]
    [Description("Test if the defer async data is fetched properly without specified partial props.")]
    public async Task TestDeferAsyncPartialDataOmitted()
    {
        var testFunction = new Func<Task<string>>(async () =>
        {
            await Task.Delay(100);
            return "Defer Async";
        });

        var response = _factory.Render("Test/Page", new
        {
            TestFunc = new Func<string>(() => "Func"),
            TestDefer = _factory.Defer(async () => await testFunction())
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testFunc" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testFunc", "Func" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        Assert.That(page?.DeferredProps, Is.EqualTo(null));
        Assert.That(page?.MergeProps, Is.EqualTo(null));
    }

    public async Task TestNoDeferredProps()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestFunc = new Func<string>(() => "Func"),
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
        Assert.That(page?.DeferredProps, Is.EqualTo(null));
        Assert.That(page?.MergeProps, Is.EqualTo(null));
    }

    [Test]
    [Description("Test if the defer data with multiple groups is fetched properly.")]
    public async Task TestDeferMultipleGroupsData()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestFunc = new Func<string>(() => "Func"),
            TestDefer = _factory.Defer(() =>
            {
                return "Defer";
            }),
            TestStats = _factory.Defer(() =>
            {
                return "Stat";
            }, "stats")
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
        Assert.That(page?.DeferredProps, Is.EqualTo(new Dictionary<string, List<string>> {
            { "default", new List<string> { "testDefer" } },
            { "stats", new List<string> { "testStats" } },
         }));
        Assert.That(page?.MergeProps, Is.EqualTo(null));
    }
}
