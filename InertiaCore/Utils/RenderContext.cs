using Microsoft.AspNetCore.Http;

namespace InertiaCore.Utils;

/// <summary>
/// Provides context information for Inertia property generation.
/// </summary>
public class RenderContext
{
    /// <summary>
    /// The name of the component being rendered.
    /// </summary>
    public string Component { get; }

    /// <summary>
    /// The current HTTP request.
    /// </summary>
    public HttpRequest Request { get; }

    /// <summary>
    /// Initializes a new instance of the RenderContext class.
    /// </summary>
    /// <param name="component">The component name</param>
    /// <param name="request">The HTTP request</param>
    public RenderContext(string component, HttpRequest request)
    {
        Component = component;
        Request = request;
    }
}