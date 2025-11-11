using InertiaCore.Models;
using InertiaCore.Utils;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if shared data is merged with the props properly.")]
    public async Task TestSharedProps()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test"
        });

        var sharedProps = new InertiaSharedProps();
        sharedProps.Set("TestShared", "Shared");

        var context = PrepareContext(null, sharedProps);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testShared", "Shared" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }

    [Test]
    [Description("Test if FlushShared clears all shared data properly.")]
    public async Task TestFlushShared()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test"
        });

        var sharedProps = new InertiaSharedProps();
        sharedProps.Set("TestShared", "Shared");
        sharedProps.Set("AnotherShared", "AnotherValue");

        var context = PrepareContext(null, sharedProps);

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testShared", "Shared" },
            { "anotherShared", "AnotherValue" },
            { "errors", new Dictionary<string, string>(0) }
        }));

        sharedProps.Clear();

        var responseAfterFlush = _factory.Render("Test/Page", new
        {
            Test = "Test"
        });

        var contextAfterFlush = PrepareContext(null, sharedProps);

        responseAfterFlush.SetContext(contextAfterFlush);
        await responseAfterFlush.ProcessResponse();

        var pageAfterFlush = responseAfterFlush.GetJson().Value as Page;

        Assert.That(pageAfterFlush?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }
}
