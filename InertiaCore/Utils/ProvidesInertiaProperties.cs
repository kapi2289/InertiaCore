using System.Collections;

namespace InertiaCore.Utils;

/// <summary>
/// Interface for objects that can provide dynamic Inertia properties.
/// </summary>
public interface ProvidesInertiaProperties
{
    /// <summary>
    /// Generates Inertia properties based on the current render context.
    /// </summary>
    /// <param name="context">The render context containing component name and request information</param>
    /// <returns>An enumerable of key-value pairs representing the properties</returns>
    IEnumerable<KeyValuePair<string, object?>> ToInertiaProperties(RenderContext context);
}