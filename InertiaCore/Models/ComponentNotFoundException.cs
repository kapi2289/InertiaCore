namespace InertiaCore.Models;

public class ComponentNotFoundException : Exception
{
    public string Component { get; }

    public ComponentNotFoundException(string component)
        : base($"Inertia page component '{component}' not found.")
    {
        Component = component;
    }

    public ComponentNotFoundException(string component, Exception innerException)
        : base($"Inertia page component '{component}' not found.", innerException)
    {
        Component = component;
    }
}