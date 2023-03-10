using InertiaCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InertiaCoreTests;

public partial class Tests
{
    /// <summary>
    /// Tests if props contain only specified partial data.
    /// </summary>
    [Test]
    public void TestPartialData()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestPartial = "Partial"
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia", "true" },
            { "X-Inertia-Partial-Data", "testPartial" },
            { "X-Inertia-Partial-Component", "Test/Page" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "testPartial", "Partial" },
            { "errors", new Dictionary<string, object?>(0) }
        }));
    }
}
