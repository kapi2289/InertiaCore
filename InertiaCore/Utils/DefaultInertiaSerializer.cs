using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace InertiaCore.Utils;

public class DefaultInertiaSerializer : IInertiaSerializer
{
    protected static JsonSerializerOptions GetOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };
    }

    public string Serialize(object? obj)
    {
        return JsonSerializer.Serialize(obj, GetOptions());
    }

    public JsonResult SerializeResult(object? obj)
    {
        return new JsonResult(obj, GetOptions());
    }
}
