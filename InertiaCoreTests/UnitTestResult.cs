using InertiaCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if the JSON result is created correctly.")]
    public void TestJsonResult()
    {
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

            Assert.That((json as Page)?.Component, Is.EqualTo("Test/Page"));
            Assert.That((json as Page)?.Props, Is.EqualTo(new Dictionary<string, object?>
            {
                { "test", "Test" },
                { "errors", new Dictionary<string, string>(0) }
            }));

            // Check the serialized JSON
            var jsonString = JsonSerializer.Serialize(json);
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonString);

            Assert.That(dictionary, Is.Not.Null);
            Assert.That(dictionary!.ContainsKey("MergeProps"), Is.False);
        });
    }

    [Test]
    [Description("Test if the JSON result with merged data is created correctly.")]
    public void TestJsonMergedResult()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test",
            TestMerged = _factory.Merge(() => "Merged")
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

            Assert.That((json as Page)?.Component, Is.EqualTo("Test/Page"));
            Assert.That((json as Page)?.Props, Is.EqualTo(new Dictionary<string, object?>
            {
                { "test", "Test" },
                { "testMerged", "Merged" },
                { "errors", new Dictionary<string, string>(0) }
            }));
            Assert.That((json as Page)?.MergeProps, Is.EqualTo(new List<string> {
                "testMerged"
            }));

            // Check the serialized JSON
            var jsonString = JsonSerializer.Serialize(json);
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonString);

            Assert.That(dictionary, Is.Not.Null);
            Assert.That(dictionary!.ContainsKey("MergeProps"), Is.True);
        });
    }

    [Test]
    [Description("Test if the view result is created correctly.")]
    public void TestViewResult()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test"
        });

        var context = PrepareContext();

        response.SetContext(context);
        response.ProcessResponse();

        var result = response.GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf(typeof(ViewResult)));
            Assert.That((result as ViewResult)?.ViewName, Is.EqualTo("~/Views/App.cshtml"));

            var model = (result as ViewResult)?.Model;
            Assert.That(model, Is.InstanceOf(typeof(Page)));

            Assert.That((model as Page)?.Component, Is.EqualTo("Test/Page"));
            Assert.That((model as Page)?.Props, Is.EqualTo(new Dictionary<string, object?>
            {
                { "test", "Test" },
                { "errors", new Dictionary<string, string>(0) }
            }));
        });
    }
}
