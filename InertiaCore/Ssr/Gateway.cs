using System.Net.Http.Json;
using System.Text;
using InertiaCore.Utils;

namespace InertiaCore.Ssr;

internal interface IGateway
{
    public Task<SsrResponse?> Dispatch(object model, string url);
}

internal class Gateway : IGateway
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IInertiaSerializer _serializer;

    public Gateway(IHttpClientFactory httpClientFactory, IInertiaSerializer serializer)
        => (_httpClientFactory, _serializer) = (httpClientFactory, serializer);

    public async Task<SsrResponse?> Dispatch(dynamic model, string url)
    {
        var json = _serializer.Serialize(model);
        var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync(url, content);
        return await response.Content.ReadFromJsonAsync<SsrResponse>();
    }
}
