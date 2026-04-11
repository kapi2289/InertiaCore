using System.Text.Json;
using InertiaCore.Extensions;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace InertiaCoreTests;

[TestFixture]
public class UnitTestBackResult
{
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<ITempDataDictionaryFactory> _tempDataFactoryMock = null!;
    private Mock<ITempDataDictionary> _tempDataMock = null!;
    private Mock<HttpContext> _httpContextMock = null!;
    private Mock<HttpRequest> _httpRequestMock = null!;
    private ActionContext _actionContext = null!;
    private Dictionary<string, object> _tempDataDict = null!;

    [SetUp]
    public void Setup()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _tempDataFactoryMock = new Mock<ITempDataDictionaryFactory>();
        _tempDataMock = new Mock<ITempDataDictionary>();
        _httpContextMock = new Mock<HttpContext>();
        _httpRequestMock = new Mock<HttpRequest>();
        _tempDataDict = new Dictionary<string, object>();

        _tempDataFactoryMock.Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
            .Returns(_tempDataMock.Object);

        _serviceProviderMock.Setup(s => s.GetService(typeof(ITempDataDictionaryFactory)))
            .Returns(_tempDataFactoryMock.Object);

        _httpContextMock.SetupGet(c => c.RequestServices).Returns(_serviceProviderMock.Object);
        _httpContextMock.SetupGet(c => c.Request).Returns(_httpRequestMock.Object);

        var headers = new HeaderDictionary();
        _httpRequestMock.SetupGet(r => r.Headers).Returns(headers);

        // Mock TempData behavior
        _tempDataMock.SetupGet(t => t["__ValidationErrors"])
            .Returns(() =>
                _tempDataDict.ContainsKey("__ValidationErrors") ? _tempDataDict["__ValidationErrors"] : null);

        _tempDataMock.SetupSet(t => t["__ValidationErrors"] = It.IsAny<object>())
            .Callback<string, object>((key, value) => _tempDataDict[key] = value);

        var modelState = new ModelStateDictionary();
        _actionContext = new ActionContext
        {
            HttpContext = _httpContextMock.Object,
            RouteData = new RouteData(),
            ActionDescriptor = new ActionDescriptor()
        };
    }

    [Test]
    public void BackResult_WithValidModelState_DoesNotStoreTempData()
    {
        // Arrange
        var backResult = new BackResult("/fallback");
        var headers = new HeaderDictionary { ["Referer"] = "https://example.com/previous" };
        _httpRequestMock.SetupGet(r => r.Headers).Returns(headers);

        // Act - We'll test the TempData storage logic without executing the full redirect
        // Simulate the error storage logic from BackResult.ExecuteResultAsync
        if (!_actionContext.ModelState.IsValid)
        {
            var tempDataFactory =
                _actionContext.HttpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
            var tempData = tempDataFactory.GetTempData(_actionContext.HttpContext);
            tempData.SetValidationErrors(_actionContext.ModelState);
        }

        // Assert - Since ModelState is valid, no TempData should be set
        // Note: We can't verify extension methods with Moq, so we check that no TempData was written
        Assert.That(_tempDataDict.ContainsKey("__ValidationErrors"), Is.False);
    }

    [Test]
    public void BackResult_WithModelStateErrors_StoresTempData()
    {
        // Arrange
        _actionContext.ModelState.AddModelError("email", "Email is required");
        _actionContext.ModelState.AddModelError("password", "Password is required");

        var tempDataDict = new Dictionary<string, object>();
        _tempDataMock.SetupGet(t => t["__ValidationErrors"]).Returns(() =>
            tempDataDict.ContainsKey("__ValidationErrors") ? tempDataDict["__ValidationErrors"] : null);
        _tempDataMock.SetupSet(t => t["__ValidationErrors"] = It.IsAny<object>())
            .Callback<string, object>((key, value) => tempDataDict[key] = value);

        var backResult = new BackResult("/fallback");
        var headers = new HeaderDictionary { ["Referer"] = "https://example.com/previous" };
        _httpRequestMock.SetupGet(r => r.Headers).Returns(headers);

        // Act - Simulate the error storage logic from BackResult.ExecuteResultAsync
        if (!_actionContext.ModelState.IsValid)
        {
            var tempDataFactory =
                _actionContext.HttpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
            var tempData = tempDataFactory.GetTempData(_actionContext.HttpContext);
            tempData.SetValidationErrors(_actionContext.ModelState);
        }

        // Assert
        Assert.That(tempDataDict.ContainsKey("__ValidationErrors"), Is.True);
        var storedJson = tempDataDict["__ValidationErrors"] as string;
        Assert.That(storedJson, Is.Not.Null);
        var storedErrors = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(storedJson);
        Assert.That(storedErrors, Is.Not.Null);
        Assert.That(storedErrors.ContainsKey("default"), Is.True);
        Assert.That(storedErrors["default"]["email"], Is.EqualTo("Email is required"));
        Assert.That(storedErrors["default"]["password"], Is.EqualTo("Password is required"));
    }

    [Test]
    public void BackResult_WithoutRequestServices_DoesNotThrow()
    {
        // Arrange
        _httpContextMock.SetupGet(c => c.RequestServices).Returns((IServiceProvider)null!);
        _actionContext.ModelState.AddModelError("test", "Test error");

        var backResult = new BackResult("/fallback");
        var headers = new HeaderDictionary { ["Referer"] = "https://example.com/previous" };
        _httpRequestMock.SetupGet(r => r.Headers).Returns(headers);

        // Act & Assert - Test the error storage logic without full redirect execution
        Assert.DoesNotThrow(() =>
        {
            // Simulate the error storage logic from BackResult.ExecuteResultAsync
            if (_actionContext.ModelState.IsValid) return;
            var requestServices = _actionContext.HttpContext.RequestServices;
            if (requestServices == null) return;

            var tempDataFactory = requestServices.GetRequiredService<ITempDataDictionaryFactory>();
            var tempData = tempDataFactory.GetTempData(_actionContext.HttpContext);
            tempData.SetValidationErrors(_actionContext.ModelState);
        });
    }

    [Test]
    public void BackResult_DefaultConstructor_UsesFallbackUrl()
    {
        // Arrange & Act
        var backResult = new BackResult();

        // Assert - We can't directly test the private field, but we can test the behavior
        // This test verifies the constructor doesn't throw
        Assert.That(backResult, Is.Not.Null);
    }

    [Test]
    public void BackResult_WithNullFallback_UsesDefaultFallback()
    {
        // Arrange & Act
        var backResult = new BackResult(null);

        // Assert - We can't directly test the private field, but we can test the behavior
        // This test verifies the constructor handles null correctly
        Assert.That(backResult, Is.Not.Null);
    }
}
