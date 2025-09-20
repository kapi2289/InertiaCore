using InertiaCore.Models;
using InertiaCore.Utils;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test With method adding a single property.")]
    public async Task TestResponseWithSingleProperty()
    {
        var response = _factory.Render("Test/Page", new
        {
            Initial = "Value"
        });

        response.With("Additional", "Property");

        var context = PrepareContext();
        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "initial", "Value" },
            { "additional", "Property" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }

    [Test]
    [Description("Test With method adding multiple properties from dictionary.")]
    public async Task TestResponseWithDictionary()
    {
        var response = _factory.Render("Test/Page", new
        {
            Initial = "Value"
        });

        var additionalProps = new Dictionary<string, object?>
        {
            { "Property1", "Value1" },
            { "Property2", 42 },
            { "Property3", true }
        };

        response.With(additionalProps);

        var context = PrepareContext();
        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "initial", "Value" },
            { "property1", "Value1" },
            { "property2", 42 },
            { "property3", true },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }

    [Test]
    [Description("Test With method adding properties from anonymous object.")]
    public async Task TestResponseWithAnonymousObject()
    {
        var response = _factory.Render("Test/Page", new
        {
            Initial = "Value"
        });

        response.With(new
        {
            Property1 = "Value1",
            Property2 = 42,
            Property3 = true
        });

        var context = PrepareContext();
        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "initial", "Value" },
            { "property1", "Value1" },
            { "property2", 42 },
            { "property3", true },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }

    [Test]
    [Description("Test With method adding ProvidesInertiaProperties object.")]
    public async Task TestResponseWithPropertyProvider()
    {
        var response = _factory.Render("Test/Page", new
        {
            Initial = "Value"
        });

        var provider = new TestPropertyProviderForWith();
        response.With(provider);

        var context = PrepareContext();
        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "initial", "Value" },
            { "providedUser", "Jane Doe" },
            { "providedRole", "admin" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }

    [Test]
    [Description("Test fluent chaining of With method.")]
    public async Task TestResponseWithFluentChaining()
    {
        var response = _factory.Render("Test/Page", new
        {
            Initial = "Value"
        });

        response
            .With("Property1", "Value1")
            .With(new { Property2 = 42 })
            .With(new Dictionary<string, object?> { { "Property3", true } });

        var context = PrepareContext();
        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "initial", "Value" },
            { "property1", "Value1" },
            { "property2", 42 },
            { "property3", true },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }

    [Test]
    [Description("Test With method overwriting existing property.")]
    public async Task TestResponseWithOverwriteProperty()
    {
        var response = _factory.Render("Test/Page", new
        {
            Initial = "Value",
            ToOverwrite = "OldValue"
        });

        response.With("ToOverwrite", "NewValue");

        var context = PrepareContext();
        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "initial", "Value" },
            { "toOverwrite", "NewValue" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }

    [Test]
    [Description("Test With method combined with WithViewData.")]
    public async Task TestResponseWithAndViewData()
    {
        var response = _factory.Render("Test/Page", new
        {
            Initial = "Value"
        });

        response
            .With("AdditionalProp", "AdditionalValue")
            .WithViewData(new Dictionary<string, object>
            {
                { "ViewDataKey", "ViewDataValue" }
            });

        var context = PrepareContext();
        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "initial", "Value" },
            { "additionalProp", "AdditionalValue" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }
}

// Test property provider for With method tests
internal class TestPropertyProviderForWith : ProvidesInertiaProperties
{
    public IEnumerable<KeyValuePair<string, object?>> ToInertiaProperties(RenderContext context)
    {
        yield return new KeyValuePair<string, object?>("providedUser", "Jane Doe");
        yield return new KeyValuePair<string, object?>("providedRole", "admin");
    }
}