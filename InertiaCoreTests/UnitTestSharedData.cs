using InertiaCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InertiaCoreTests;

public partial class Tests
{
    /// <summary>
    /// Test if shared data is merged with the props properly.
    /// </summary>
    [Test]
    public void TestSharedData()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test"
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia", "true" },
        };

        var sharedData = new InertiaSharedData();
        sharedData.Set("TestShared", "Shared");

        var context = PrepareContext(headers, sharedData);

        response.SetContext(context);
        response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "testShared", "Shared" },
            { "errors", new Dictionary<string, object?>(0) }
        }));
    }
}
