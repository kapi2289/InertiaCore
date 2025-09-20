using InertiaCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InertiaCoreTests;

public partial class Tests
{
    [Test]
    [Description("Test if custom URL resolver is used when provided.")]
    public async Task TestCustomUrlResolver()
    {
        // Set up a custom URL resolver
        _factory.ResolveUrlUsing(context => "/custom/url");

        var response = _factory.Render("Test/Page", new
        {
            Test = "Test"
        });

        var context = PrepareContext();

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Url, Is.EqualTo("/custom/url"));
        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }

    [Test]
    [Description("Test if default URL resolver is used when no custom resolver is provided.")]
    public async Task TestDefaultUrlResolver()
    {
        var response = _factory.Render("Test/Page", new
        {
            Test = "Test"
        });

        var context = PrepareContext();

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        // Should use the default RequestedUri() method
        Assert.That(page?.Url, Is.Not.Null);
        Assert.That(page?.Props, Is.EqualTo(new Dictionary<string, object?>
        {
            { "test", "Test" },
            { "errors", new Dictionary<string, string>(0) }
        }));
    }

    [Test]
    [Description("Test if custom URL resolver receives correct ActionContext.")]
    public async Task TestUrlResolverReceivesContext()
    {
        ActionContext? receivedContext = null;

        // Set up a custom URL resolver that captures the context
        _factory.ResolveUrlUsing(context =>
        {
            receivedContext = context;
            return "/captured/context/url";
        });

        var response = _factory.Render("Test/Page", new
        {
            Test = "Test"
        });

        var context = PrepareContext();

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Url, Is.EqualTo("/captured/context/url"));
        Assert.That(receivedContext, Is.Not.Null);
        Assert.That(receivedContext, Is.EqualTo(context));
    }

    [Test]
    [Description("Test if custom URL resolver can access request information.")]
    public async Task TestUrlResolverAccessesRequest()
    {
        // Set up a custom URL resolver that uses request path
        _factory.ResolveUrlUsing(context =>
        {
            var path = context.HttpContext.Request.Path;
            return $"/custom{path}";
        });

        var response = _factory.Render("Test/Page", new
        {
            Test = "Test"
        });

        var context = PrepareContext();

        response.SetContext(context);
        await response.ProcessResponse();

        var page = response.GetJson().Value as Page;

        Assert.That(page?.Url, Is.Not.Null);
        Assert.That(page?.Url, Does.StartWith("/custom"));
    }

    [Test]
    [Description("Test if URL resolver can be changed between requests.")]
    public async Task TestUrlResolverCanBeChanged()
    {
        // First resolver
        _factory.ResolveUrlUsing(context => "/first/url");

        var response1 = _factory.Render("Test/Page", new { Test = "Test1" });
        var context1 = PrepareContext();

        response1.SetContext(context1);
        await response1.ProcessResponse();

        var page1 = response1.GetJson().Value as Page;
        Assert.That(page1?.Url, Is.EqualTo("/first/url"));

        // Change resolver
        _factory.ResolveUrlUsing(context => "/second/url");

        var response2 = _factory.Render("Test/Page", new { Test = "Test2" });
        var context2 = PrepareContext();

        response2.SetContext(context2);
        await response2.ProcessResponse();

        var page2 = response2.GetJson().Value as Page;
        Assert.That(page2?.Url, Is.EqualTo("/second/url"));
    }
}