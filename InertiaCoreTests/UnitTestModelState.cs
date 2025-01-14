using InertiaCore.Models;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if the model state dictionary is passed to the props correctly.")]
    public async Task TestModelState()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test"
        });

        var context = PrepareContext(null, null, new Dictionary<string, string>
        {
            { "Field", "Error" }
        });

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            {
                "errors", new Dictionary<string, string>
                {
                    { "field", "Error" }
                }
            }
        }));
    }
}
