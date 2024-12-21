using InertiaCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if history encryption is sent correctly.")]
    public void TestHistoryEncryptionResult()
    {
        _factory.EncryptHistory();

        var response = _factory.Render("Test/Page", new
        {
            Test = "Test"
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia", "true" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        response.ProcessResponse();

        var result = response.GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf(typeof(JsonResult)));

            var json = (result as JsonResult)?.Value;
            Assert.That(json, Is.InstanceOf(typeof(Page)));

            Assert.That((json as Page)?.ClearHistory, Is.EqualTo(false));
            Assert.That((json as Page)?.EncryptHistory, Is.EqualTo(true));
            Assert.That((json as Page)?.Component, Is.EqualTo("Test/Page"));
            Assert.That((json as Page)?.Props, Is.EqualTo(new Dictionary<string, object?>
            {
                { "test", "Test" },
                { "errors", new Dictionary<string, string>(0) }
            }));
        });
    }

    [Test]
    [Description("Test if clear history is sent correctly.")]
    public void TestClearHistoryResult()
    {
        _factory.ClearHistory();

        var response = _factory.Render("Test/Page", new
        {
            Test = "Test"
        });

        var headers = new HeaderDictionary
        {
            { "X-Inertia", "true" }
        };

        var context = PrepareContext(headers);

        response.SetContext(context);
        response.ProcessResponse();

        var result = response.GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf(typeof(JsonResult)));

            var json = (result as JsonResult)?.Value;
            Assert.That(json, Is.InstanceOf(typeof(Page)));

            Assert.That((json as Page)?.ClearHistory, Is.EqualTo(true));
            Assert.That((json as Page)?.EncryptHistory, Is.EqualTo(false));
            Assert.That((json as Page)?.Component, Is.EqualTo("Test/Page"));
            Assert.That((json as Page)?.Props, Is.EqualTo(new Dictionary<string, object?>
            {
                { "test", "Test" },
                { "errors", new Dictionary<string, string>(0) }
            }));
        });
    }
}
