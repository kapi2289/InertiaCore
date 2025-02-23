using InertiaCore.Models;
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

}
