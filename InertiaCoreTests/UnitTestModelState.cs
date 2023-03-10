using InertiaCore;
using Microsoft.AspNetCore.Http;

namespace InertiaCoreTests;

public partial class Tests
{
    /// <summary>
    /// Tests if the model state dictionary is passed to props correctly.
    /// </summary>
    [Test]
    public void TestModelState()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test"
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia", "true" },
        };

        var context = PrepareContext(headers, null, new Dictionary<string, string>
        {
            { "Field", "Error" }
        });

        response.SetContext(context);
        response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            {
                "errors", new Dictionary<string, object>
                {
                    { "field", "Error" }
                }
            }
        }));
    }
}
