using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InertiaCore.Ssr;

internal interface IGateway
{
    public Task<SsrResponse?> Dispatch(object model, string url);
}

internal class Gateway : IGateway
{
    private readonly IHttpClientFactory _httpClientFactory;

    public Gateway(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task<SsrResponse?> Dispatch(dynamic model, string url)
    {
        var json = JsonSerializer.Serialize(model,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            });
        var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync(url, content);
        return await response.Content.ReadFromJsonAsync<SsrResponse>();
    }
}
