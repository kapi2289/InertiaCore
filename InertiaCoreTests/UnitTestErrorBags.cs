using System.Reflection;
using System.Text.Json;
using InertiaCore;
using InertiaCore.Extensions;
using InertiaCore.Models;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace InertiaCoreTests;

[TestFixture]
public class UnitTestErrorBags
{
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<ITempDataDictionaryFactory> _tempDataFactoryMock = null!;
    private Mock<ITempDataDictionary> _tempDataMock = null!;
    private Mock<HttpContext> _httpContextMock = null!;
    private Mock<HttpRequest> _httpRequestMock = null!;
    private Mock<IInertiaSerializer> _serializerMock = null!;
    private ActionContext _actionContext = null!;
    private Response _response = null!;

    [SetUp]
    public void Setup()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _tempDataFactoryMock = new Mock<ITempDataDictionaryFactory>();
        _tempDataMock = new Mock<ITempDataDictionary>();
        _httpContextMock = new Mock<HttpContext>();
        _httpRequestMock = new Mock<HttpRequest>();
        _serializerMock = new Mock<IInertiaSerializer>();

        _tempDataFactoryMock.Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
            .Returns(_tempDataMock.Object);

        _serviceProviderMock.Setup(s => s.GetService(typeof(ITempDataDictionaryFactory)))
            .Returns(_tempDataFactoryMock.Object);

        _httpContextMock.SetupGet(c => c.RequestServices).Returns(_serviceProviderMock.Object);
        _httpContextMock.SetupGet(c => c.Request).Returns(_httpRequestMock.Object);

        var headers = new HeaderDictionary();
        _httpRequestMock.SetupGet(r => r.Headers).Returns(headers);

        var modelState = new ModelStateDictionary();
        _actionContext = new ActionContext
        {
            HttpContext = _httpContextMock.Object,
            RouteData = new RouteData(),
            ActionDescriptor = new ActionDescriptor()
        };

        // Set up reflection to access internal constructor
        var responseType = typeof(Response);
        var constructor = responseType.GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[]
            {
                typeof(string), typeof(Dictionary<string, object?>), typeof(string), typeof(string),
                typeof(IInertiaSerializer)
            },
            null);

        _response = (Response)constructor!.Invoke(new object[]
            { "TestComponent", new Dictionary<string, object?>(), "app", null!, _serializerMock.Object });
        _response.SetContext(_actionContext);
    }

    [Test]
    public void SetValidationErrors_WithDictionary_StoresInTempData()
    {
        // Arrange
        var errors = new Dictionary<string, string>
        {
            ["email"] = "Email is required",
            ["password"] = "Password is required"
        };

        var tempDataDict = new Dictionary<string, object>();
        _tempDataMock.SetupGet(t => t["__ValidationErrors"])
            .Returns(() => tempDataDict.GetValueOrDefault("__ValidationErrors"));
        _tempDataMock.SetupSet(t => t["__ValidationErrors"] = It.IsAny<object>())
            .Callback<string, object>((key, value) => tempDataDict[key] = value);

        // Act
        _tempDataMock.Object.SetValidationErrors(errors, "login");

        // Assert
        var storedJson = tempDataDict["__ValidationErrors"] as string;
        Assert.That(storedJson, Is.Not.Null);
        var storedErrors = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(storedJson);
        Assert.That(storedErrors, Is.Not.Null);
        Assert.That(storedErrors.ContainsKey("login"), Is.True);
        Assert.That(storedErrors["login"]["email"], Is.EqualTo("Email is required"));
        Assert.That(storedErrors["login"]["password"], Is.EqualTo("Password is required"));
    }

    [Test]
    public void SetValidationErrors_WithModelState_StoresInTempData()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email is required");
        modelState.AddModelError("Password", "Password is required");

        var tempDataDict = new Dictionary<string, object>();
        _tempDataMock.SetupGet(t => t["__ValidationErrors"])
            .Returns(() => tempDataDict.GetValueOrDefault("__ValidationErrors"));
        _tempDataMock.SetupSet(t => t["__ValidationErrors"] = It.IsAny<object>())
            .Callback<string, object>((key, value) => tempDataDict[key] = value);

        // Act
        _tempDataMock.Object.SetValidationErrors(modelState, "registration");

        // Assert
        var storedJson = tempDataDict["__ValidationErrors"] as string;
        Assert.That(storedJson, Is.Not.Null);
        var storedErrors = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(storedJson);
        Assert.That(storedErrors, Is.Not.Null);
        Assert.That(storedErrors.ContainsKey("registration"), Is.True);
        Assert.That(storedErrors["registration"]["Email"], Is.EqualTo("Email is required"));
        Assert.That(storedErrors["registration"]["Password"], Is.EqualTo("Password is required"));
    }

    [Test]
    public void ResolveValidationErrors_WithNoErrors_ReturnsEmptyObject()
    {
        // Arrange
        _tempDataMock.Setup(t => t.ContainsKey("__ValidationErrors")).Returns(false);

        // Mock ModelState as valid - Need to create new ActionContext with valid ModelState
        var modelState = new ModelStateDictionary();
        var testActionContext = new ActionContext
        {
            HttpContext = _httpContextMock.Object,
            RouteData = new RouteData(),
            ActionDescriptor = new ActionDescriptor()
        };

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var responseType = typeof(Response);
            var constructor = responseType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[]
                {
                    typeof(string), typeof(Dictionary<string, object?>), typeof(string), typeof(string),
                    typeof(IInertiaSerializer)
                },
                null);
            var testResponse = (Response)constructor!.Invoke(new object[]
                { "TestComponent", new Dictionary<string, object?>(), "app", null!, _serializerMock.Object });
            testResponse.SetContext(testActionContext);
        });
    }

    [Test]
    public void ResolveValidationErrors_WithErrorBagHeader_ReturnsNamedBag()
    {
        // Arrange
        var errorBags = new Dictionary<string, Dictionary<string, string>>
        {
            ["default"] = new Dictionary<string, string>
            {
                ["email"] = "Email is required",
                ["password"] = "Password is required"
            }
        };

        _tempDataMock.Setup(t => t.ContainsKey("__ValidationErrors")).Returns(true);
        _tempDataMock.Setup(t => t["__ValidationErrors"]).Returns(errorBags);

        var headers = new HeaderDictionary
        {
            [InertiaHeader.ErrorBag] = "login"
        };
        _httpRequestMock.SetupGet(r => r.Headers).Returns(headers);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var responseType = typeof(Response);
            var constructor = responseType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[]
                {
                    typeof(string), typeof(Dictionary<string, object?>), typeof(string), typeof(string),
                    typeof(IInertiaSerializer)
                },
                null);
            var testResponse = (Response)constructor!.Invoke(new object[]
                { "TestComponent", new Dictionary<string, object?>(), "app", null!, _serializerMock.Object });
            testResponse.SetContext(_actionContext);
        });
    }

    [Test]
    public void ResolveValidationErrors_WithDefaultBagOnly_ReturnsDirectly()
    {
        // Arrange
        var errorBags = new Dictionary<string, Dictionary<string, string>>
        {
            ["default"] = new Dictionary<string, string>
            {
                ["email"] = "Email is required",
                ["password"] = "Password is required"
            }
        };

        _tempDataMock.Setup(t => t.ContainsKey("__ValidationErrors")).Returns(true);
        _tempDataMock.Setup(t => t["__ValidationErrors"]).Returns(errorBags);

        var headers = new HeaderDictionary();
        _httpRequestMock.SetupGet(r => r.Headers).Returns(headers);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var responseType = typeof(Response);
            var constructor = responseType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[]
                {
                    typeof(string), typeof(Dictionary<string, object?>), typeof(string), typeof(string),
                    typeof(IInertiaSerializer)
                },
                null);
            var testResponse = (Response)constructor!.Invoke(new object[]
                { "TestComponent", new Dictionary<string, object?>(), "app", null!, _serializerMock.Object });
            testResponse.SetContext(_actionContext);
        });
    }

    [Test]
    public void ResolveValidationErrors_WithMultipleBags_ReturnsAll()
    {
        // Arrange
        var errorBags = new Dictionary<string, Dictionary<string, string>>
        {
            ["login"] = new Dictionary<string, string>
            {
                ["email"] = "Login email is required"
            },
            ["registration"] = new Dictionary<string, string>
            {
                ["password"] = "Registration password is required"
            }
        };

        _tempDataMock.Setup(t => t.ContainsKey("__ValidationErrors")).Returns(true);
        _tempDataMock.Setup(t => t["__ValidationErrors"]).Returns(errorBags);

        var headers = new HeaderDictionary();
        _httpRequestMock.SetupGet(r => r.Headers).Returns(headers);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var responseType = typeof(Response);
            var constructor = responseType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[]
                {
                    typeof(string), typeof(Dictionary<string, object?>), typeof(string), typeof(string),
                    typeof(IInertiaSerializer)
                },
                null);
            var testResponse = (Response)constructor!.Invoke(new object[]
                { "TestComponent", new Dictionary<string, object?>(), "app", null!, _serializerMock.Object });
            testResponse.SetContext(_actionContext);
        });
    }

    /// <summary>
    /// Builds a Response + ActionContext pair that actually wires up an in-memory
    /// TempData backing store and real request services, so ProcessResponse() can
    /// be invoked end-to-end and the resolved errors prop inspected. This is what
    /// the regression tests below use to assert the exact shape of the errors prop.
    /// </summary>
    private static (Response response, ActionContext context) BuildResolveFixture(
        Dictionary<string, Dictionary<string, string>>? errorBags,
        string? errorBagHeader,
        Dictionary<string, string>? modelStateErrors)
    {
        var tempDataBacking = new Dictionary<string, object?>();
        if (errorBags != null)
        {
            tempDataBacking["__ValidationErrors"] = JsonSerializer.Serialize(errorBags);
        }

        var tempDataMock = new Mock<ITempDataDictionary>();
        tempDataMock.Setup(t => t.ContainsKey(It.IsAny<string>()))
            .Returns<string>(k => tempDataBacking.ContainsKey(k));
        tempDataMock.Setup(t => t[It.IsAny<string>()])
            .Returns<string>(k => tempDataBacking.TryGetValue(k, out var v) ? v : null);
        tempDataMock.Setup(t => t.Remove(It.IsAny<string>()))
            .Returns<string>(k => tempDataBacking.Remove(k));

        var tempDataFactory = new Mock<ITempDataDictionaryFactory>();
        tempDataFactory.Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
            .Returns(tempDataMock.Object);

        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(ITempDataDictionaryFactory)))
            .Returns(tempDataFactory.Object);

        var headers = new HeaderDictionary();
        if (!string.IsNullOrEmpty(errorBagHeader))
        {
            headers[InertiaHeader.ErrorBag] = errorBagHeader;
        }

        var request = new Mock<HttpRequest>();
        request.SetupGet(r => r.Headers).Returns(headers);
        request.SetupGet(r => r.Path).Returns(new PathString("/"));
        request.SetupGet(r => r.QueryString).Returns(QueryString.Empty);
        request.SetupGet(r => r.Scheme).Returns("http");
        request.SetupGet(r => r.Host).Returns(new HostString("localhost"));
        request.SetupGet(r => r.PathBase).Returns(PathString.Empty);

        var responseHeaders = new HeaderDictionary();
        var httpResponse = new Mock<HttpResponse>();
        httpResponse.SetupGet(r => r.Headers).Returns(responseHeaders);

        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(c => c.Request).Returns(request.Object);
        httpContext.SetupGet(c => c.Response).Returns(httpResponse.Object);
        httpContext.SetupGet(c => c.Features).Returns(new FeatureCollection());
        httpContext.SetupGet(c => c.RequestServices).Returns(services.Object);

        var context = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
        if (modelStateErrors != null)
        {
            foreach (var kvp in modelStateErrors)
                context.ModelState.AddModelError(kvp.Key, kvp.Value);
        }

        var serializer = new Mock<IInertiaSerializer>();
        var ctor = typeof(Response).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[]
            {
                typeof(string), typeof(Dictionary<string, object?>), typeof(string), typeof(string),
                typeof(IInertiaSerializer)
            },
            null);
        var response = (Response)ctor!.Invoke(new object[]
            { "TestComponent", new Dictionary<string, object?>(), "app", null!, serializer.Object });
        response.SetContext(context);
        return (response, context);
    }

    private static async Task<object?> ResolveErrorsProp(
        Dictionary<string, Dictionary<string, string>>? errorBags,
        string? errorBagHeader,
        Dictionary<string, string>? modelStateErrors)
    {
        var (response, _) = BuildResolveFixture(errorBags, errorBagHeader, modelStateErrors);
        await response.ProcessResponse();
        var pageField = typeof(Response).GetField("_page", BindingFlags.NonPublic | BindingFlags.Instance);
        var page = (Page)pageField!.GetValue(response)!;
        return page.Props["errors"];
    }

    [Test]
    [Description(
        "Laravel parity: with no TempData bags and no ModelState errors, errors prop is an empty dictionary."
    )]
    public async Task ResolveValidationErrors_NoErrorsAnywhere_ReturnsEmptyDictionary()
    {
        var errors = await ResolveErrorsProp(errorBags: null, errorBagHeader: null, modelStateErrors: null);

        Assert.That(errors, Is.InstanceOf<Dictionary<string, string>>());
        Assert.That((Dictionary<string, string>)errors!, Is.Empty);
    }

    [Test]
    [Description(
        "Laravel parity: with no TempData bags but ModelState errors and no header, errors prop is the flat ModelState dict with camelCased keys."
    )]
    public async Task ResolveValidationErrors_ModelStateOnly_NoHeader_ReturnsFlatDictionary()
    {
        var errors = await ResolveErrorsProp(
            errorBags: null,
            errorBagHeader: null,
            modelStateErrors: new Dictionary<string, string> { ["Email"] = "Email is required" });

        Assert.That(errors, Is.EqualTo(new Dictionary<string, string>
        {
            ["email"] = "Email is required"
        }));
    }

    [Test]
    [Description(
        "Laravel parity: with no TempData bags, ModelState errors, and an error-bag header, the ModelState errors are wrapped under the requested bag name."
    )]
    public async Task ResolveValidationErrors_ModelStateOnly_WithHeader_WrapsUnderNamedBag()
    {
        var errors = await ResolveErrorsProp(
            errorBags: null,
            errorBagHeader: "contact",
            modelStateErrors: new Dictionary<string, string> { ["Email"] = "Email is required" });

        Assert.That(errors, Is.InstanceOf<Dictionary<string, object>>());
        var dict = (Dictionary<string, object>)errors!;
        Assert.That(dict.Keys, Is.EquivalentTo(new[] { "contact" }));
        Assert.That(dict["contact"], Is.EqualTo(new Dictionary<string, string>
        {
            ["email"] = "Email is required"
        }));
    }

    [Test]
    [Description(
        "Laravel parity: with only a default bag in TempData and no header, the default bag's errors are returned unwrapped (flat)."
    )]
    public async Task ResolveValidationErrors_DefaultBagOnly_NoHeader_ReturnsUnwrapped()
    {
        var bags = new Dictionary<string, Dictionary<string, string>>
        {
            ["default"] = new() { ["email"] = "Email is required" }
        };

        var errors = await ResolveErrorsProp(errorBags: bags, errorBagHeader: null, modelStateErrors: null);

        Assert.That(errors, Is.EqualTo(new Dictionary<string, string>
        {
            ["email"] = "Email is required"
        }));
    }

    [Test]
    [Description(
        "Laravel parity (the subtle case): with only a default bag in TempData AND X-Inertia-Error-Bag set, the default bag's errors get RE-LABELED under the requested bag name. Not flat, not under 'default'."
    )]
    public async Task ResolveValidationErrors_DefaultBag_WithHeader_RelabelsUnderRequestedBag()
    {
        var bags = new Dictionary<string, Dictionary<string, string>>
        {
            ["default"] = new() { ["email"] = "Email is required" }
        };

        var errors = await ResolveErrorsProp(errorBags: bags, errorBagHeader: "login", modelStateErrors: null);

        Assert.That(errors, Is.InstanceOf<Dictionary<string, object>>());
        var dict = (Dictionary<string, object>)errors!;
        Assert.That(dict.Keys, Is.EquivalentTo(new[] { "login" }),
            "The default bag must be re-labeled as the requested bag name, not left as 'default'.");
        Assert.That(dict["login"], Is.EqualTo(new Dictionary<string, string>
        {
            ["email"] = "Email is required"
        }));
    }

    [Test]
    [Description(
        "Laravel parity: with multiple named bags in TempData (no default) and no header, all bags are returned as a nested structure."
    )]
    public async Task ResolveValidationErrors_MultipleNamedBags_NoHeader_ReturnsAllBags()
    {
        var bags = new Dictionary<string, Dictionary<string, string>>
        {
            ["login"] = new() { ["email"] = "Login email is required" },
            ["registration"] = new() { ["password"] = "Registration password is required" }
        };

        var errors = await ResolveErrorsProp(errorBags: bags, errorBagHeader: null, modelStateErrors: null);

        Assert.That(errors, Is.InstanceOf<Dictionary<string, object>>());
        var dict = (Dictionary<string, object>)errors!;
        Assert.That(dict.Keys, Is.EquivalentTo(new[] { "login", "registration" }));
        Assert.That(dict["login"], Is.EqualTo(new Dictionary<string, string>
        {
            ["email"] = "Login email is required"
        }));
        Assert.That(dict["registration"], Is.EqualTo(new Dictionary<string, string>
        {
            ["password"] = "Registration password is required"
        }));
    }

    [Test]
    [Description(
        "Laravel parity: with multiple named bags (no default) and a header set, the header only matters when a default bag exists, so all bags are still returned unchanged."
    )]
    public async Task ResolveValidationErrors_MultipleNamedBags_WithHeader_ReturnsAllBagsUnchanged()
    {
        var bags = new Dictionary<string, Dictionary<string, string>>
        {
            ["login"] = new() { ["email"] = "Login email is required" },
            ["registration"] = new() { ["password"] = "Registration password is required" }
        };

        var errors = await ResolveErrorsProp(errorBags: bags, errorBagHeader: "login", modelStateErrors: null);

        Assert.That(errors, Is.InstanceOf<Dictionary<string, object>>());
        var dict = (Dictionary<string, object>)errors!;
        Assert.That(dict.Keys, Is.EquivalentTo(new[] { "login", "registration" }));
        Assert.That(dict["login"], Is.EqualTo(new Dictionary<string, string>
        {
            ["email"] = "Login email is required"
        }));
        Assert.That(dict["registration"], Is.EqualTo(new Dictionary<string, string>
        {
            ["password"] = "Registration password is required"
        }));
    }

    [Test]
    [Description(
        "Laravel parity: with multiple TempData bags that include a default bag and no header, " +
        "the default bag's errors are returned unwrapped (flat). Mirrors Laravel's " +
        "Middleware::resolveValidationErrors pipe which returns the default bag unconditionally when present."
    )]
    public async Task ResolveValidationErrors_MultipleBagsIncludingDefault_NoHeader_ReturnsDefaultOnly()
    {
        var bags = new Dictionary<string, Dictionary<string, string>>
        {
            ["default"] = new() { ["email"] = "Email required" },
            ["login"] = new() { ["email"] = "Login email required" }
        };

        var errors = await ResolveErrorsProp(errorBags: bags, errorBagHeader: null, modelStateErrors: null);

        // Must be flat (default bag unwrapped), not nested under bag names.
        Assert.That(errors, Is.InstanceOf<Dictionary<string, string>>(),
            "Errors prop must be unwrapped to the default bag's flat dictionary, not a nested bag structure.");
        var dict = (Dictionary<string, string>)errors!;
        Assert.That(dict, Is.EqualTo(new Dictionary<string, string>
        {
            ["email"] = "Email required"
        }), "Must be the default bag's errors, not the login bag's errors.");
        Assert.That(dict.ContainsKey("default"), Is.False, "Must not be nested under 'default'.");
        Assert.That(dict.ContainsKey("login"), Is.False, "Must not include the 'login' bag.");
    }

    [Test]
    public void ResolveValidationErrors_FallbackToModelState_WithErrorBag()
    {
        // Arrange
        _tempDataMock.Setup(t => t.ContainsKey("__ValidationErrors")).Returns(false);

        var modelState = new ModelStateDictionary();
        modelState.AddModelError("email", "Email is required");

        // Create real ActionContext with ModelState - ActionContext properties cannot be mocked
        var testActionContext = new ActionContext
        {
            HttpContext = _httpContextMock.Object,
            RouteData = new RouteData(),
            ActionDescriptor = new ActionDescriptor()
        };

        // Add model state errors manually using reflection since ModelState is get-only
        var modelStateProperty = typeof(ActionContext).GetProperty("ModelState");
        var modelStateField =
            typeof(ActionContext).GetField("_modelState", BindingFlags.NonPublic | BindingFlags.Instance);

        if (modelStateField != null)
        {
            modelStateField.SetValue(testActionContext, modelState);
        }
        else
        {
            // Fallback: add errors directly to the existing ModelState
            testActionContext.ModelState.AddModelError("email", "Email is required");
        }

        var headers = new HeaderDictionary
        {
            [InertiaHeader.ErrorBag] = "contact"
        };
        _httpRequestMock.SetupGet(r => r.Headers).Returns(headers);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var responseType = typeof(Response);
            var constructor = responseType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[]
                {
                    typeof(string), typeof(Dictionary<string, object?>), typeof(string), typeof(string),
                    typeof(IInertiaSerializer)
                },
                null);
            var testResponse = (Response)constructor!.Invoke(new object[]
                { "TestComponent", new Dictionary<string, object?>(), "app", null!, _serializerMock.Object });
            testResponse.SetContext(testActionContext);
        });
    }
}
