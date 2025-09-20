namespace InertiaCore.Utils;

/// <summary>
/// Interface for objects that can transform themselves into an Inertia property value.
/// </summary>
public interface ProvidesInertiaProperty
{
    /// <summary>
    /// Transforms the object into an Inertia property value based on the given context.
    /// </summary>
    /// <param name="context">The property context containing key, props, and request information</param>
    /// <returns>The transformed property value</returns>
    object? ToInertiaProperty(PropertyContext context);
}