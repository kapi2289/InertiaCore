using InertiaCore;
using InertiaCore.Models;
using InertiaCore.Ssr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace InertiaCoreTests;

[TestFixture]
public class UnitTestHistoryEncryption
{
    [SetUp]
    public void Setup()
    {
        // Set up a factory for testing
        var options = new InertiaOptions();
        var factory = new ResponseFactory(
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IGateway>().Object,
            Options.Create(options)
        );

        Inertia.UseFactory(factory);
    }

    [Test]
    public void Inertia_EncryptHistory_MethodExists()
    {
        // This test verifies the API exists and can be called
        Assert.DoesNotThrow(() => Inertia.EncryptHistory(true));
        Assert.DoesNotThrow(() => Inertia.EncryptHistory(false));
        Assert.DoesNotThrow(() => Inertia.EncryptHistory());
    }

    [Test]
    public void Inertia_ClearHistory_MethodExists()
    {
        // This test verifies the API exists and can be called
        Assert.DoesNotThrow(() => Inertia.ClearHistory(true));
        Assert.DoesNotThrow(() => Inertia.ClearHistory(false));
        Assert.DoesNotThrow(() => Inertia.ClearHistory());
    }


    [Test]
    public void Page_HasHistoryEncryptionProperties()
    {
        // This test verifies the Page model has the required properties
        var page = new Page();

        Assert.That(page.EncryptHistory, Is.False); // Default value
        Assert.That(page.ClearHistory, Is.False);   // Default value

        page.EncryptHistory = true;
        page.ClearHistory = true;

        Assert.That(page.EncryptHistory, Is.True);
        Assert.That(page.ClearHistory, Is.True);
    }

    [Test]
    public void InertiaOptions_HasEncryptHistoryProperty()
    {
        // This test verifies the options class has the required property
        var options = new InertiaOptions();

        Assert.That(options.EncryptHistory, Is.False); // Default value

        options.EncryptHistory = true;
        Assert.That(options.EncryptHistory, Is.True);
    }
}