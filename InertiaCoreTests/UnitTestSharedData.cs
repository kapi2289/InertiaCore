using InertiaCore.Models;
using InertiaCore.Utils;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if shared data is merged with the props properly.")]
    public async Task TestSharedData()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test"
        });

        var sharedData = new InertiaSharedData();
        sharedData.Set("TestShared", "Shared");

        var context = PrepareContext(null, sharedData);

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
}
