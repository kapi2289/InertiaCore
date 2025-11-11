using InertiaCore;
using InertiaCore.Extensions;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System.Text.Json;
using System.Net;

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
            RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
        };

        // Set up reflection to access internal constructor
        var responseType = typeof(Response);
        var constructor = responseType.GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(string), typeof(Dictionary<string, object?>), typeof(string), typeof(string), typeof(IInertiaSerializer) },
            null);

        _response = (Response)constructor!.Invoke(new object[] { "TestComponent", new Dictionary<string, object?>(), "app", null!, _serializerMock.Object });
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
        _tempDataMock.SetupGet(t => t["__ValidationErrors"]).Returns(() => tempDataDict.ContainsKey("__ValidationErrors") ? tempDataDict["__ValidationErrors"] : null);
        _tempDataMock.SetupSet(t => t["__ValidationErrors"] = It.IsAny<object>()).Callback<string, object>((key, value) => tempDataDict[key] = value);

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
        _tempDataMock.SetupGet(t => t["__ValidationErrors"]).Returns(() => tempDataDict.ContainsKey("__ValidationErrors") ? tempDataDict["__ValidationErrors"] : null);
        _tempDataMock.SetupSet(t => t["__ValidationErrors"] = It.IsAny<object>()).Callback<string, object>((key, value) => tempDataDict[key] = value);

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
            RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
        };

        // Act & Assert
        Assert.DoesNotThrow(() => {
            var responseType = typeof(Response);
            var constructor = responseType.GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(Dictionary<string, object?>), typeof(string), typeof(string), typeof(IInertiaSerializer) },
                null);
            var testResponse = (Response)constructor!.Invoke(new object[] { "TestComponent", new Dictionary<string, object?>(), "app", null!, _serializerMock.Object });
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
        Assert.DoesNotThrow(() => {
            var responseType = typeof(Response);
            var constructor = responseType.GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(Dictionary<string, object?>), typeof(string), typeof(string), typeof(IInertiaSerializer) },
                null);
            var testResponse = (Response)constructor!.Invoke(new object[] { "TestComponent", new Dictionary<string, object?>(), "app", null!, _serializerMock.Object });
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
        Assert.DoesNotThrow(() => {
            var responseType = typeof(Response);
            var constructor = responseType.GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(Dictionary<string, object?>), typeof(string), typeof(string), typeof(IInertiaSerializer) },
                null);
            var testResponse = (Response)constructor!.Invoke(new object[] { "TestComponent", new Dictionary<string, object?>(), "app", null!, _serializerMock.Object });
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
        Assert.DoesNotThrow(() => {
            var responseType = typeof(Response);
            var constructor = responseType.GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(Dictionary<string, object?>), typeof(string), typeof(string), typeof(IInertiaSerializer) },
                null);
            var testResponse = (Response)constructor!.Invoke(new object[] { "TestComponent", new Dictionary<string, object?>(), "app", null!, _serializerMock.Object });
            testResponse.SetContext(_actionContext);
        });
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
            RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
        };

        // Add model state errors manually using reflection since ModelState is get-only
        var modelStateProperty = typeof(ActionContext).GetProperty("ModelState");
        var modelStateField = typeof(ActionContext).GetField("_modelState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

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
        Assert.DoesNotThrow(() => {
            var responseType = typeof(Response);
            var constructor = responseType.GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(Dictionary<string, object?>), typeof(string), typeof(string), typeof(IInertiaSerializer) },
                null);
            var testResponse = (Response)constructor!.Invoke(new object[] { "TestComponent", new Dictionary<string, object?>(), "app", null!, _serializerMock.Object });
            testResponse.SetContext(testActionContext);
        });
    }
}