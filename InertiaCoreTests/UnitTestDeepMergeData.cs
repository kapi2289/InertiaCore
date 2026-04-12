using InertiaCore.Models;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Http;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if the deep merge data is fetched properly.")]
    public async Task TestDeepMergeData()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestFunc = new Func<string>(() => "Func"),
            TestDeepMerge = _factory.DeepMerge(() =>
            {
                return "Deep Merge";
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
            { "testDeepMerge", "Deep Merge" },
            { "errors", new Dictionary<string, string>(0) }
        }));
        Assert.That(page?.DeepMergeProps, Is.EqualTo(new List<string> { "testDeepMerge" }));
    }

    [Test]
    [Description("Test if the deep merge data is fetched properly with specified partial props.")]
    public async Task TestDeepMergePartialData()
    {
        var response = _factory.Render("Test/Page", new
        {
            TestFunc = new Func<string>(() => "Func"),
            TestDeepMerge = _factory.DeepMerge(() => "Deep Merge")
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testFunc,testDeepMerge" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testFunc", "Func" },
            { "testDeepMerge", "Deep Merge" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        Assert.That(page?.DeepMergeProps, Is.EqualTo(new List<string> { "testDeepMerge" }));
    }

    [Test]
    [Description("Test if the deep merge async data is fetched properly.")]
    public async Task TestDeepMergeAsyncData()
    {
        var testFunction = new Func<Task<object?>>(async () =>
        {
            await Task.Delay(100);
            return "Deep Merge Async";
        });

        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestFunc = new Func<string>(() => "Func"),
            TestDeepMerge = _factory.DeepMerge(testFunction)
        });

        var context = PrepareContext();

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testFunc", "Func" },
            { "testDeepMerge", "Deep Merge Async" },
            { "errors", new Dictionary<string, string>(0) }
        }));
        Assert.That(page?.DeepMergeProps, Is.EqualTo(new List<string> { "testDeepMerge" }));
    }

    [Test]
    [Description("Test if the deep merge data is fetched properly without specified partial props.")]
    public async Task TestDeepMergePartialDataOmitted()
    {
        var testFunction = new Func<Task<string>>(async () =>
        {
            await Task.Delay(100);
            return "Deep Merge Async";
        });

        var response = _factory.Render("Test/Page", new
        {
            TestFunc = new Func<string>(() => "Func"),
            TestDeepMerge = _factory.DeepMerge(async () => await testFunction())
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

        Assert.That(page?.DeepMergeProps, Is.EqualTo(null));
    }

    [Test]
    [Description("Test if there are no deep merge props when none are specified.")]
    public async Task TestNoDeepMergeProps()
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
        Assert.That(page?.DeepMergeProps, Is.EqualTo(null));
    }

    [Test]
    [Description("Test if deep merge props are excluded when using PARTIAL_EXCEPT header.")]
    public async Task TestDeepMergePropsWithPartialExcept()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestDeepMerge1 = _factory.DeepMerge(() => "Deep Merge1"),
            TestDeepMerge2 = _factory.DeepMerge(() => "Deep Merge2"),
            TestNormal = "Normal"
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Except", "testDeepMerge1" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testDeepMerge2", "Deep Merge2" },
            { "testNormal", "Normal" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        // testDeepMerge1 should be excluded from deep merge props due to PARTIAL_EXCEPT header
        Assert.That(page?.DeepMergeProps, Is.EqualTo(new List<string> { "testDeepMerge2" }));
    }

    [Test]
    [Description("Test if only specified deep merge props are included when using PARTIAL_ONLY header.")]
    public async Task TestDeepMergePropsWithPartialOnly()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestDeepMerge1 = _factory.DeepMerge(() => "Deep Merge1"),
            TestDeepMerge2 = _factory.DeepMerge(() => "Deep Merge2"),
            TestNormal = "Normal"
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testDeepMerge1,testNormal" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testDeepMerge1", "Deep Merge1" },
            { "testNormal", "Normal" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        // Only testDeepMerge1 should be in deep merge props since testDeepMerge2 was not included in PARTIAL_ONLY
        Assert.That(page?.DeepMergeProps, Is.EqualTo(new List<string> { "testDeepMerge1" }));
    }

    [Test]
    [Description("Test if deep merge props work with match on keys.")]
    public async Task TestDeepMergeWithMatchOn()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestDeepMerge1 = ((Mergeable)_factory.DeepMerge("Deep Merge1")).MatchesOn("deep"),
            TestDeepMerge2 = ((Mergeable)_factory.DeepMerge(() => "Deep Merge2")).MatchesOn("shallow", "replace"),
            TestNormal = "Normal"
        });

        var context = PrepareContext();

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testDeepMerge1", "Deep Merge1" },
            { "testDeepMerge2", "Deep Merge2" },
            { "testNormal", "Normal" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        Assert.That(page?.DeepMergeProps, Is.EqualTo(new List<string> { "testDeepMerge1", "testDeepMerge2" }));
        // Deep merge props should also appear in match props on since they inherit from Mergeable
        Assert.That(page?.MatchPropsOn, Is.EqualTo(new List<string>
        {
            "testDeepMerge1.deep",
            "testDeepMerge2.shallow",
            "testDeepMerge2.replace"
        }));
    }

    [Test]
    [Description("Test if regular merge and deep merge props coexist properly.")]
    public async Task TestMergeAndDeepMergeCoexistence()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestMerge = _factory.Merge(() => "Regular Merge"),
            TestDeepMerge = _factory.DeepMerge(() => "Deep Merge"),
            TestNormal = "Normal"
        });

        var context = PrepareContext();

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testMerge", "Regular Merge" },
            { "testDeepMerge", "Deep Merge" },
            { "testNormal", "Normal" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        Assert.That(page?.MergeProps, Is.EqualTo(new List<string> { "testMerge" }));
        Assert.That(page?.DeepMergeProps, Is.EqualTo(new List<string> { "testDeepMerge" }));
    }
}