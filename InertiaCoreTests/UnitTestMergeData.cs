using InertiaCore.Models;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Http;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if the merge data is fetched properly.")]
    public async Task TestMergeData()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestFunc = new Func<string>(() => "Func"),
            TestMerge = _factory.Merge(() =>
            {
                return "Merge";
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
            { "testMerge", "Merge" },
            { "errors", new Dictionary<string, string>(0) }
        }));
        Assert.That(page?.MergeProps, Is.EqualTo(new List<string> { "testMerge" }));
    }

    [Test]
    [Description("Test if the merge data is fetched properly with specified partial props.")]
    public async Task TestMergePartialData()
    {
        var response = _factory.Render("Test/Page", new
        {
            TestFunc = new Func<string>(() => "Func"),
            TestMerge = _factory.Merge(() => "Merge")
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testFunc,testMerge" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testFunc", "Func" },
            { "testMerge", "Merge" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        Assert.That(page?.MergeProps, Is.EqualTo(new List<string> { "testMerge" }));
    }

    [Test]
    [Description("Test if the merge async data is fetched properly.")]
    public async Task TestMergeAsyncData()
    {
        var testFunction = new Func<Task<object?>>(async () =>
        {
            await Task.Delay(100);
            return "Merge Async";
        });

        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestFunc = new Func<string>(() => "Func"),
            TestMerge = _factory.Merge(testFunction)
        });

        var context = PrepareContext();

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testFunc", "Func" },
            { "testMerge", "Merge Async" },
            { "errors", new Dictionary<string, string>(0) }
        }));
        Assert.That(page?.MergeProps, Is.EqualTo(new List<string> { "testMerge" }));
    }

    [Test]
    [Description("Test if the merge async data is fetched properly with specified partial props.")]
    public async Task TestMergeAsyncPartialData()
    {
        var testFunction = new Func<Task<string>>(async () =>
        {
            await Task.Delay(100);
            return "Merge Async";
        });

        var response = _factory.Render("Test/Page", new
        {
            TestFunc = new Func<string>(() => "Func"),
            TestMerge = _factory.Merge(async () => await testFunction())
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testFunc,testMerge" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testFunc", "Func" },
            { "testMerge", "Merge Async" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        Assert.That(page?.MergeProps, Is.EqualTo(new List<string> { "testMerge" }));
    }

    [Test]
    [Description("Test if the merge async data is fetched properly without specified partial props.")]
    public async Task TestMergeAsyncPartialDataOmitted()
    {
        var testFunction = new Func<Task<string>>(async () =>
        {
            await Task.Delay(100);
            return "Merge Async";
        });

        var response = _factory.Render("Test/Page", new
        {
            TestFunc = new Func<string>(() => "Func"),
            TestMerge = _factory.Merge(async () => await testFunction())
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

        Assert.That(page?.MergeProps, Is.EqualTo(null));
    }

    [Test]
    [Description("Test if the merge async data is fetched properly without specified partial props.")]
    public async Task TestNoMergeProps()
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
        Assert.That(page?.MergeProps, Is.EqualTo(null));
    }

    [Test]
    [Description("Test if merge props are excluded when using PARTIAL_EXCEPT header.")]
    public async Task TestMergePropsWithPartialExcept()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestMerge1 = _factory.Merge(() => "Merge1"),
            TestMerge2 = _factory.Merge(() => "Merge2"),
            TestNormal = "Normal"
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Except", "testMerge1" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testMerge2", "Merge2" },
            { "testNormal", "Normal" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        // testMerge1 should be excluded from merge props due to PARTIAL_EXCEPT header
        Assert.That(page?.MergeProps, Is.EqualTo(new List<string> { "testMerge2" }));
    }

    [Test]
    [Description("Test if only specified merge props are included when using PARTIAL_ONLY header.")]
    public async Task TestMergePropsWithPartialOnly()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestMerge1 = _factory.Merge(() => "Merge1"),
            TestMerge2 = _factory.Merge(() => "Merge2"),
            TestNormal = "Normal"
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testMerge1,testNormal" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testMerge1", "Merge1" },
            { "testNormal", "Normal" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        // Only testMerge1 should be in merge props since testMerge2 was not included in PARTIAL_ONLY
        Assert.That(page?.MergeProps, Is.EqualTo(new List<string> { "testMerge1" }));
    }

    [Test]
    [Description("Test if merge props respect both PARTIAL_ONLY and PARTIAL_EXCEPT headers.")]
    public async Task TestMergePropsWithPartialOnlyAndExcept()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestMerge1 = _factory.Merge(() => "Merge1"),
            TestMerge2 = _factory.Merge(() => "Merge2"),
            TestMerge3 = _factory.Merge(() => "Merge3"),
            TestNormal = "Normal"
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testMerge1,testMerge2,testMerge3,testNormal" },
            { "X-Inertia-Partial-Except", "testMerge2" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testMerge1", "Merge1" },
            { "testMerge3", "Merge3" },
            { "testNormal", "Normal" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        // testMerge1 and testMerge3 should be in merge props (testMerge2 excluded by PARTIAL_EXCEPT)
        Assert.That(page?.MergeProps, Is.EqualTo(new List<string> { "testMerge1", "testMerge3" }));
    }

    [Test]
    [Description("Test if match props on are resolved properly for merge props.")]
    public async Task TestMatchPropsOn()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestMerge1 = ((Mergeable)_factory.Merge("Merge1")).MatchesOn("deep"),
            TestMerge2 = ((Mergeable)_factory.Merge(() => "Merge2")).MatchesOn("shallow", "replace"),
            TestNormal = "Normal"
        });

        var context = PrepareContext();

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testMerge1", "Merge1" },
            { "testMerge2", "Merge2" },
            { "testNormal", "Normal" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        Assert.That(page?.MergeProps, Is.EqualTo(new List<string> { "testMerge1", "testMerge2" }));
        Assert.That(page?.MatchPropsOn, Is.EqualTo(new List<string>
        {
            "testMerge1.deep",
            "testMerge2.shallow",
            "testMerge2.replace"
        }));
    }

    [Test]
    [Description("Test if match props on are handled properly with partial props.")]
    public async Task TestMatchPropsOnWithPartialProps()
    {
        var response = _factory.Render("Test/Page", new
        {
            TestMerge1 = ((Mergeable)_factory.Merge("Merge1")).MatchesOn("deep"),
            TestMerge2 = ((Mergeable)_factory.Merge(() => "Merge2")).MatchesOn("shallow", "replace"),
            TestMerge3 = ((Mergeable)_factory.Merge("Merge3")).MatchesOn("custom")
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Data", "testMerge1,testMerge3" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testMerge1", "Merge1" },
            { "testMerge3", "Merge3" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        Assert.That(page?.MergeProps, Is.EqualTo(new List<string> { "testMerge1", "testMerge3" }));
        Assert.That(page?.MatchPropsOn, Is.EqualTo(new List<string>
        {
            "testMerge1.deep",
            "testMerge3.custom"
        }));
    }

    [Test]
    [Description("Test if match props on are excluded when using PARTIAL_EXCEPT header.")]
    public async Task TestMatchPropsOnWithPartialExcept()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestMerge1 = ((Mergeable)_factory.Merge("Merge1")).MatchesOn("deep"),
            TestMerge2 = ((Mergeable)_factory.Merge(() => "Merge2")).MatchesOn("shallow", "replace"),
            TestNormal = "Normal"
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Partial-Except", "testMerge1" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testMerge2", "Merge2" },
            { "testNormal", "Normal" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        Assert.That(page?.MergeProps, Is.EqualTo(new List<string> { "testMerge2" }));
        Assert.That(page?.MatchPropsOn, Is.EqualTo(new List<string>
        {
            "testMerge2.shallow",
            "testMerge2.replace"
        }));
    }

    [Test]
    [Description("Test if match props on are null when no merge props have match keys.")]
    public async Task TestNoMatchPropsOn()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestMerge = _factory.Merge(() => "Merge"), // No strategies
            TestNormal = "Normal"
        });

        var context = PrepareContext();

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testMerge", "Merge" },
            { "testNormal", "Normal" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        Assert.That(page?.MergeProps, Is.EqualTo(new List<string> { "testMerge" }));
        Assert.That(page?.MatchPropsOn, Is.EqualTo(null));
    }

    [Test]
    [Description("Test if merge props honor the X-Inertia-Reset header.")]
    public async Task TestMergePropsWithResetHeader()
    {
        var response = _factory.Render("Test/Page", new
        {
            TestMerge1 = _factory.Merge(() => "Merge1"),
            TestMerge2 = _factory.Merge(() => "Merge2"),
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia-Reset", "testMerge1" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        // testMerge1 should NOT appear in mergeProps because it's in the reset list
        Assert.That(page?.MergeProps, Is.EqualTo(new List<string> { "testMerge2" }));
    }

}
