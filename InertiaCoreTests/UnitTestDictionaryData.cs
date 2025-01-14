using InertiaCore.Models;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if all nested dictionaries and its values are resolved properly.")]
    public async Task TestDictionaryData()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestDict = new Dictionary<string, object>
            {
                ["Key"] = () => "Value",
                ["KeyAsync"] = () => Task.FromResult("ValueAsync"),
                ["KeyAsync2"] = Task.FromResult("ValueAsync2"),
                ["Always"] = () => new Dictionary<string, object>
                {
                    ["Key"] = () => "Value"
                }
            }
        });

        var context = PrepareContext();
        response.SetContext(context);

        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            {
                "testDict", new Dictionary<string, object?>
                {
                    { "key", "Value" },
                    { "keyAsync", "ValueAsync" },
                    { "keyAsync2", "ValueAsync2" },
                    {
                        "always", new Dictionary<string, object?>
                        {
                            { "key", "Value" },
                        }
                    }
                }
            },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }
}
