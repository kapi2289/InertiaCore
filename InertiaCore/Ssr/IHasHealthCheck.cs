namespace InertiaCore.Ssr;

internal interface IHasHealthCheck
{
    public Task<bool> IsHealthy();
}