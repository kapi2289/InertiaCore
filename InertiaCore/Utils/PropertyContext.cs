using Microsoft.AspNetCore.Http;

namespace InertiaCore.Utils;

/// <summary>
/// Provides context information for individual property transformation.
/// </summary>
public class PropertyContext
{
    /// <summary>
    /// The key of the property being transformed.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// All properties in the response.
    /// </summary>
    public Dictionary<string, object?> Props { get; }

    /// <summary>
    /// The current HTTP request.
    /// </summary>
    public HttpRequest Request { get; }

    /// <summary>
    /// Initializes a new instance of the PropertyContext class.
    /// </summary>
    /// <param name="key">The property key</param>
    /// <param name="props">All properties</param>
    /// <param name="request">The HTTP request</param>
    public PropertyContext(string key, Dictionary<string, object?> props, HttpRequest request)
    {
        Key = key;
        Props = props;
        Request = request;
    }
}