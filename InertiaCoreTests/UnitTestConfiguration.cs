using System.Text.Json;
using System.Text.Json.Serialization;
using InertiaCore;
using InertiaCore.Extensions;
using InertiaCore.Ssr;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace InertiaCoreTests;

internal class DummySerializer : DefaultInertiaSerializer
{
    protected new static JsonSerializerOptions GetOptions()
    {
        return DefaultInertiaSerializer.GetOptions();
    }
}

public partial class Tests
{
    [Test]
    [Description("Test if the configuration registers properly necessary services and filters.")]
    public void TestConfiguration()
    {
        Assert.Throws<NullReferenceException>(() => Inertia.GetVersion());

        var builder = WebApplication.CreateBuilder();

        Assert.DoesNotThrow(() => builder.Services.AddInertia());

        Assert.Multiple(() =>
        {
            Assert.That(builder.Services.Any(s => s.ServiceType == typeof(IHttpContextAccessor)), Is.True);
            Assert.That(builder.Services.Any(s => s.ServiceType == typeof(IHttpClientFactory)), Is.True);

            Assert.That(builder.Services.Any(s => s.ServiceType == typeof(IResponseFactory)), Is.True);
            Assert.That(builder.Services.Any(s => s.ServiceType == typeof(IGateway)), Is.True);
            Assert.That(builder.Services.Any(s => s.ServiceType == typeof(IInertiaSerializer)), Is.True);
        });

        var mvcConfiguration =
            builder.Services.FirstOrDefault(s => s.ServiceType == typeof(IConfigureOptions<MvcOptions>));

        var mvcOptions = new MvcOptions();
        (mvcConfiguration?.ImplementationInstance as ConfigureNamedOptions<MvcOptions>)?.Action?.Invoke(mvcOptions);

        Assert.That(
            mvcOptions.Filters.Any(f => (f as TypeFilterAttribute)?.ImplementationType == typeof(InertiaActionFilter)),
            Is.True);

        var app = builder.Build();
        Assert.DoesNotThrow(() => app.UseInertia());

        Assert.DoesNotThrow(() => Inertia.GetVersion());
    }

    [Test]
    [Description("Test if the configuration registers properly custom JSON serializer.")]
    public void TestSerializerConfiguration()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddInertia();

        Assert.Multiple(() =>
        {
            Assert.That(builder.Services.Any(s => s.ServiceType == typeof(IInertiaSerializer)), Is.True);

            Assert.That(builder.Services.Any(s => s.ImplementationType == typeof(DefaultInertiaSerializer)), Is.True);
            Assert.That(builder.Services.Any(s => s.ImplementationType == typeof(DummySerializer)), Is.False);
        });

        Assert.DoesNotThrow(() => builder.Services.UseInertiaSerializer<DummySerializer>());

        Assert.Multiple(() =>
        {
            Assert.That(builder.Services.Any(s => s.ServiceType == typeof(IInertiaSerializer)), Is.True);

            Assert.That(builder.Services.Any(s => s.ImplementationType == typeof(DefaultInertiaSerializer)), Is.False);
            Assert.That(builder.Services.Any(s => s.ImplementationType == typeof(DummySerializer)), Is.True);
        });
    }
}
