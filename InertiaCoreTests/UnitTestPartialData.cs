using InertiaCore.Models;
using Microsoft.AspNetCore.Http;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if the props contain only the specified partial data.")]
    public void TestPartialData()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestPartial = "Partial",
            TestFunc = new Func<string>(() =>
            {
                Assert.Fail();
                return "Func";
            }),
            TestLazy = _factory.Lazy(() =>
            {
                Assert.Fail();
                return "Lazy";
            })
        });

        var headers = new HeaderDictionary
        {
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
            { "errors", new Dictionary<string, string>(0) }
        }));
    }
}
